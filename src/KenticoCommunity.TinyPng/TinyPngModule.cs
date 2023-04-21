using CMS;
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
                case AttachmentHistoryInfo attachmentHistory:
                    ShrinkAttachmentHistory(attachmentHistory, settings);
                    break;

                case AttachmentInfo attachment:
                    ShrinkAttachment(attachment, settings);
                    break;

                case MediaFileInfo mediaFile:
                    ShrinkMediaFile(mediaFile, settings);
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

    private void ShrinkAttachmentHistory(AttachmentHistoryInfo newAttachmentHistory, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(newAttachmentHistory.AttachmentExtension, settings))
        {
            return;
        }

        var lastVersion = AttachmentHistoryInfo.Provider.Get()
            .WhereEquals("AttachmentGUID", newAttachmentHistory.AttachmentGUID)
            .OrderByDescending("AttachmentLastModified")
            .Columns(
                nameof(AttachmentHistoryInfo.AttachmentGUID),
                nameof(AttachmentHistoryInfo.AttachmentLastModified),
                nameof(AttachmentHistoryInfo.AttachmentSize))
            .TopN(1)
            .FirstOrDefault();

        if (AttachmentsAreSame(newAttachmentHistory, lastVersion))
        {
            return;
        }

        var newBinary = Shrink(newAttachmentHistory.AttachmentBinary, settings);
        newAttachmentHistory.AttachmentBinary = newBinary;
        newAttachmentHistory.AttachmentSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private void ShrinkAttachment(AttachmentInfo newAttachment, ITinyPngSettingsProvider settings)
    {
        if (!IsExtensionAllowed(newAttachment.AttachmentExtension, settings))
        {
            return;
        }

        var document = DocumentHelper.GetDocument(newAttachment.AttachmentDocumentID, new TreeProvider());

        // If the attachment is on a document under workflow, we should ignore and let attachment history take over.
        if (document.WorkflowStep != null)
        {
            return;
        }

        var lastAttachment = AttachmentInfo.Provider.GetWithoutBinary(newAttachment.AttachmentID);

        // If the attachment is the same, then ignore so we don't re-shrink the same image.
        if (AttachmentsAreSame(newAttachment, lastAttachment))
        {
            return;
        }

        var newBinary = Shrink(newAttachment.AttachmentBinary, settings);
        newAttachment.AttachmentBinary = newBinary;
        newAttachment.AttachmentSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private static bool AttachmentsAreSame(IAttachment a, IAttachment b)
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
        mediaFile.FileBinary = newBinary;
        mediaFile.FileSize = ValidationHelper.GetInteger(newBinary.Length, 0);
    }

    private static byte[] GetMediaBinary(MediaFileInfo mediaFile)
    {
        return MediaFileInfoProvider.GetFile(mediaFile, MediaLibraryInfo.Provider.Get(mediaFile.FileLibraryID).LibraryFolder, SiteInfoProvider.GetSiteName(mediaFile.FileSiteID));
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
