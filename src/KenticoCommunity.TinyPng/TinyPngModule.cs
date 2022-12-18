﻿using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.MediaLibrary;
using CMS.SiteProvider;
using KenticoCommunity.TinyPng;
using TinifyAPI;

[assembly: AssemblyDiscoverable]
[assembly: RegisterModule(typeof(TinyPngModule))]

namespace KenticoCommunity.TinyPng;

public class TinyPngModule : Module
{
    public TinyPngModule() : base(nameof(TinyPngModule), false)
    {
    }

    protected override void OnInit()
    {
        base.OnInit();

        Service.Resolve<IEventLogService>().LogInformation(nameof(TinyPngModule), nameof(OnInit), "Module initializing.");

        AttachmentHistoryInfo.TYPEINFO.Events.Insert.Before += BeforeFileSave;

        AttachmentInfo.TYPEINFO.Events.Insert.Before += BeforeFileSave;
        AttachmentInfo.TYPEINFO.Events.Update.Before += BeforeFileSave;

        MediaFileInfo.TYPEINFO.Events.Insert.Before += BeforeFileSave;
        MediaFileInfo.TYPEINFO.Events.Update.Before += BeforeFileSave;

        MetaFileInfo.TYPEINFO.Events.Insert.Before += BeforeFileSave;
        MetaFileInfo.TYPEINFO.Events.Update.Before += BeforeFileSave;
    }

    private void BeforeFileSave(object sender, ObjectEventArgs e)
    {
        try
        {
            // If there wasn't another provider registered, use the default AppSettings provider.
            var settings = Service.ResolveOptional<ITinyPngSettingsProvider>() ?? new TinyPngSettingsFromAppSettingsProvider();

            if (!settings.IsEnabled)
            {
                return;
            }

            switch (e.Object)
            {
                // Used when workflow is enabled on the attachment's page
                case AttachmentHistoryInfo attachmentHistory:
                    ShrinkAttachmentHistory(attachmentHistory, settings);
                    break;

                // Used when workflow is not enabled on the attachment's page
                case AttachmentInfo attachment:
                    ShrinkAttachment(attachment, settings);
                    break;

                // Media files
                case MediaFileInfo mediaFile:
                    ShrinkMediaFile(mediaFile, settings);
                    break;

                // Meta files
                case MetaFileInfo metaFile:
                    ShrinkMetaFile(metaFile, settings);
                    break;

                default:
                    return;
            }
        }
        catch (Exception ex)
        {
            Service.Resolve<IEventLogService>().LogException(nameof(TinyPngModule), "EXCEPTION", ex, SiteContext.CurrentSiteID);
        }
    }

    private void ShrinkAttachmentHistory(AttachmentHistoryInfo attachmentHistory, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(attachmentHistory.AttachmentExtension, settings))
        {
            return;
        }

        var latest = AttachmentHistoryInfo.Provider.Get()
            .WhereEquals("AttachmentGUID", attachmentHistory.AttachmentGUID)
            .OrderByDescending("AttachmentLastModified")
            .Columns(
                nameof(AttachmentHistoryInfo.AttachmentGUID),
                nameof(AttachmentHistoryInfo.AttachmentLastModified),
                nameof(AttachmentHistoryInfo.AttachmentSize))
            .TopN(1)
            .FirstOrDefault();

        if (IsSame(attachmentHistory, latest))
        {
            return;
        }

        var newBinary = Shrink(attachmentHistory.AttachmentBinary, settings);
        attachmentHistory.AttachmentBinary = newBinary.ToArray();
        attachmentHistory.AttachmentSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private void ShrinkAttachment(AttachmentInfo attachment, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(attachment.AttachmentExtension, settings))
        {
            return;
        }

        var document = DocumentHelper.GetDocument(attachment.AttachmentDocumentID, new TreeProvider());

        // If the attachment is on a document under workflow, we should ignore and let attachment history take over.
        if (document.WorkflowStep != null)
        {
            return;
        }

        var currentAttachment = AttachmentInfo.Provider.GetWithoutBinary(attachment.AttachmentID);

        // If the attachment is the same, then ignore so we don't re-shrink the same image.
        if (IsSame(attachment, currentAttachment))
        {
            return;
        }

        var newBinary = Shrink(attachment.AttachmentBinary, settings);
        attachment.AttachmentBinary = newBinary.ToArray();
        attachment.AttachmentSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private static bool IsSame(IAttachment a, IAttachment b)
    {
        if (a is null || b is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(a.AttachmentHash) && !string.IsNullOrWhiteSpace(b.AttachmentHash))
        {
            return a.AttachmentHash == b.AttachmentHash;
        }

        // If no hash, use size. It's not perfect, but probably unlikely to result in a false positive.
        return a.AttachmentSize == b.AttachmentSize;
    }

    private void ShrinkMediaFile(MediaFileInfo mediaFile, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(mediaFile.FileExtension, settings))
        {
            return;
        }

        var originalBinary = mediaFile.FileBinary ?? GetMediaBinary(mediaFile);

        var newBinary = Shrink(originalBinary, settings);
        mediaFile.FileBinary = newBinary.ToArray();
        mediaFile.FileSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private static byte[] GetMediaBinary(MediaFileInfo mediaFile)
    {
        return MediaFileInfoProvider.GetFile(mediaFile, MediaLibraryInfo.Provider.Get(mediaFile.FileLibraryID).LibraryFolder, SiteInfoProvider.GetSiteName(mediaFile.FileSiteID));
    }

    private void ShrinkMetaFile(MetaFileInfo metaFile, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(metaFile.MetaFileExtension, settings))
        {
            return;
        }

        var originalBinary = metaFile.MetaFileBinary ?? GetMetaBinary(metaFile);

        var newBinary = Shrink(originalBinary, settings);
        metaFile.MetaFileBinary = newBinary.ToArray();
        metaFile.MetaFileSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private static byte[] GetMetaBinary(MetaFileInfo metaFile)
    {
        return MetaFileInfoProvider.GetFile(metaFile, SiteInfoProvider.GetSiteName(metaFile.MetaFileSiteID));
    }

    private bool IsExtensionAllowed(string fileExtension, ITinyPngSettingsProvider settings)
    {
        return !string.IsNullOrWhiteSpace(fileExtension)
            && settings.AllowedExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
    }

    private byte[] Shrink(byte[] originalBinary, ITinyPngSettingsProvider settings)
    {
        Tinify.Key = settings.ApiKey;
        return Task.Run(() => Tinify.FromBuffer(originalBinary).ToBuffer()).GetAwaiter().GetResult();
    }
}
