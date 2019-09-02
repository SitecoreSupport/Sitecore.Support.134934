namespace Sitecore.Support.Shell.Applications.Media.Imager
{
    // Sitecore.Shell.Applications.Media.Imager.ImagerForm
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.IO;
    using Sitecore.Resources;
    using Sitecore.Shell.Framework;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Web.UI.WebControls.Ribbons;
    using System;
    using System.Drawing;
    using System.Web.UI.WebControls;

    /// <summary>
    /// Represents the Imager form.
    /// </summary>
    public class ImagerForm : BaseForm
    {
        /// <summary></summary>
        protected Sitecore.Web.UI.HtmlControls.Image Image;

        /// <summary></summary>
        protected Sitecore.Web.UI.HtmlControls.Action HasFile;

        /// <summary></summary>
        protected Border RibbonPanel;

        /// <summary>
        /// Gets or sets the current language.
        /// </summary>
        /// <value>The current language.</value>
        public string CurrentLanguage
        {
            get
            {
                string @string = StringUtil.GetString(Context.ClientPage.ServerProperties["Language"]);
                if (!string.IsNullOrEmpty(@string))
                {
                    return @string;
                }
                return Context.Language.ToString();
            }
            set
            {
                Context.ClientPage.ServerProperties["Language"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the current version.
        /// </summary>
        /// <value>The current version.</value>
        public string CurrentVersion
        {
            get
            {
                string @string = StringUtil.GetString(Context.ClientPage.ServerProperties["Version"]);
                if (!string.IsNullOrEmpty(@string))
                {
                    return @string;
                }
                return Sitecore.Data.Version.Latest.ToString();
            }
            set
            {
                Context.ClientPage.ServerProperties["Version"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        public string File
        {
            get
            {
                return StringUtil.GetString(Context.ClientPage.ServerProperties["File"]);
            }
            set
            {
                Context.ClientPage.ServerProperties["File"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        /// <value>The item ID.</value>
        public string ItemID
        {
            get
            {
                return StringUtil.GetString(Context.ClientPage.ServerProperties["ItemID"]);
            }
            set
            {
                Context.ClientPage.ServerProperties["ItemID"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the parent frame.
        /// </summary>
        /// <value>The name of the parent frame.</value>
        public string ParentFrameName
        {
            get
            {
                return StringUtil.GetString(ServerProperties["ParentFrameName"]);
            }
            set
            {
                ServerProperties["ParentFrameName"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the undo list.
        /// </summary>
        /// <value>The undo list.</value>
        public string UndoList
        {
            get
            {
                return StringUtil.GetString(Context.ClientPage.ServerProperties["Undo"]);
            }
            set
            {
                Context.ClientPage.ServerProperties["Undo"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the work.
        /// </summary>
        /// <value>The work.</value>
        public int Work
        {
            get
            {
                return (int)Context.ClientPage.ServerProperties["Work"];
            }
            set
            {
                Context.ClientPage.ServerProperties["Work"] = value;
            }
        }

        /// <summary>
        /// Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Error.AssertObject(message, "message");
            Item item = null;
            if (!string.IsNullOrEmpty(ItemID))
            {
                item = Context.ContentDatabase.GetItem(ItemID, Language.Parse(CurrentLanguage), Sitecore.Data.Version.Parse(CurrentVersion));
            }
            switch (message.Name)
            {
                case "imager:load":
                    LoadImage(message["id"], Language.Current, Sitecore.Data.Version.Latest);
                    UpdateRibbon();
                    return;
                case "imager:undochange":
                    Undo();
                    return;
                case "imager:redochange":
                    Redo();
                    return;
                case "imager:cropimage":
                    Crop();
                    return;
                case "imager:resizeimage":
                    Context.ClientPage.Start(this, "Resize");
                    return;
                case "imager:rotateimage":
                    Rotate(message["Direction"]);
                    return;
                case "imager:flipimage":
                    Flip(message["Direction"]);
                    return;
            }
            base.HandleMessage(message);
            CommandContext commandContext = new CommandContext(item);
            if (Work >= 0)
            {
                commandContext.Parameters["WorkFile"] = GetWorkFile(Work);
            }
            if (!string.IsNullOrEmpty(ParentFrameName))
            {
                commandContext.Parameters["ParentFramename"] = ParentFrameName;
            }
            commandContext.Parameters["HasFile"] = (HasFile.Disabled ? "0" : "1");
            commandContext.Parameters["File"] = File;
            if (message.Name == "item:save")
            {
                message = Message.Parse(this, message.ToString().Replace("item:save", "imager:save"));
                message.Arguments["alert"] = "0";
            }
            Dispatcher.Dispatch(message, commandContext);
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> instance containing the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
            {
                return;
            }
            ItemID = string.Empty;
            UndoList = string.Empty;
            Work = -1;
            File = string.Empty;
            string queryString = WebUtil.GetQueryString("id");
            if (queryString.Length > 0)
            {
                string queryString2 = WebUtil.GetQueryString("la");
                string queryString3 = WebUtil.GetQueryString("vs");
                LoadImage(queryString, Language.Parse(queryString2), Sitecore.Data.Version.Parse(queryString3));
            }
            else
            {
                string queryString4 = WebUtil.GetQueryString("fi");
                if (!string.IsNullOrEmpty(queryString4))
                {
                    LoadImageFromFile(queryString4);
                }
            }
            ParentFrameName = WebUtil.GetQueryString("pfn");
            UpdateRibbon();
        }

        /// <summary>
        /// Crops this instance.
        /// </summary>
        protected void Crop()
        {
            string workFile = GetWorkFile(Work);
            string text = Context.ClientPage.ClientRequest.Form["CropInfo"];
            if (text != null && text.Length > 0)
            {
                string[] array = text.Split(',');
                int @int = MainUtil.GetInt(array[0], 0);
                int int2 = MainUtil.GetInt(array[1], 0);
                int int3 = MainUtil.GetInt(array[2], 0);
                int int4 = MainUtil.GetInt(array[3], 0);
                if (int3 > 0 && int4 > 0)
                {
                    string src = Imager.Crop(File, workFile, @int, int2, int3, int4);
                    Update(src, int3, int4, true);
                }
            }
        }

        /// <summary>
        /// Flips the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        protected void Flip(string direction)
        {
            string workFile = GetWorkFile(Work);
            string src = Imager.Mirror(File, workFile, direction);
            Update(src, 0, 0, true);
        }

        /// <summary>
        /// Redoes this instance.
        /// </summary>
        protected void Redo()
        {
            ListString listString = new ListString(UndoList, '|');
            if (Work < listString.Count - 1)
            {
                Work++;
                string file;
                int width;
                int height;
                GetUndo(Work, out file, out width, out height);
                Update(file, width, height, false);
            }
        }

        /// <summary>
        /// Resizes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        protected void Resize(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (args.Result != null && args.Result.Length > 0 && args.Result != "undefined")
                {
                    string workFile = GetWorkFile(Work);
                    string[] array = args.Result.Split(',');
                    int @int = MainUtil.GetInt(array[0], 0);
                    int int2 = MainUtil.GetInt(array[1], 0);
                    if (@int > 0 && int2 > 0)
                    {
                        string src = Imager.Resize(File, workFile, @int, int2);
                        Update(src, @int, int2, true);
                    }
                }
            }
            else
            {
                string workFile2 = GetWorkFile(Work);
                UrlString urlString = new UrlString(UIUtil.GetUri("control:ImagerResize"));
                urlString.Append("wo", workFile2);
                SheerResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Rotates the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        protected void Rotate(string direction)
        {
            string workFile = GetWorkFile(Work);
            int width;
            int height;
            string src = Imager.Rotate(File, workFile, direction, out width, out height);
            Update(src, width, height, true);
        }

        /// <summary>
        /// Performs undo.
        /// </summary>
        protected void Undo()
        {
            if (Work > 0)
            {
                Work--;
                string file;
                int width;
                int height;
                GetUndo(Work, out file, out width, out height);
                Update(file, width, height, false);
            }
        }

        /// <summary>
        /// Adds the undo information.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="undo">if set to <c>true</c> this instance is undo.</param>
        private void AddUndo(string src, int width, int height, bool undo)
        {
            Work++;
            string value = src + "*" + width + "*" + height + "*" + (undo ? "1" : "0");
            ListString listString = new ListString(UndoList, '|');
            while (listString.Count > Work)
            {
                listString.Remove(listString.Count - 1);
            }
            listString.Add(value);
            UndoList = listString.ToString();
        }

        /// <summary>
        /// Gets the work file.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        private string GetWorkFile(int index)
        {
            ListString listString = new ListString(UndoList, '|');
            string text = listString[index];
            #region Modified code
            text = text.Substring(0, text.IndexOf("*", StringComparison.InvariantCulture));
            // the full path to the file is taken if the prefix is "/-/temp/".
            if ((text[0] != '/') || !StringUtil.RemovePrefix("/", text).StartsWith("-/temp/", StringComparison.InvariantCulture))
            {
                return text;
            }
            return FileUtilSupport.MapPathWithTempRequestPrefix(text);
            #endregion
        }

        /// <summary>
        /// Gets the undo information.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="file">The file.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        private void GetUndo(int index, out string file, out int width, out int height)
        {
            ListString listString = new ListString(UndoList, '|');
            string text = listString[index];
            string[] array = text.Split('*');
            file = array[0];
            width = MainUtil.GetInt(array[1], 0);
            height = MainUtil.GetInt(array[2], 0);
        }

        /// <summary>
        /// Loads the image.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="language">The language.</param>
        /// <param name="version">The version.</param>
        private void LoadImage(string id, Language language, Sitecore.Data.Version version)
        {
            MediaItem mediaItem = Context.ContentDatabase.GetItem(ID.Parse(id), language, version);
            if (mediaItem == null)
            {
                SheerResponse.ShowError("This image item is not associated with an image file.", string.Empty);
                return;
            }
            ItemID = mediaItem.ID.ToString();
            CurrentLanguage = language.ToString();
            CurrentVersion = version.ToString();
            UndoList = string.Empty;
            Work = -1;
            string text = mediaItem.InnerItem["width"];
            string text2 = mediaItem.InnerItem["height"];
            if (text.Length > 0 && text2.Length > 0)
            {
                File = Imager.CreateTemporaryFile(mediaItem);
                if (File.Length > 0)
                {
                    AddUndo(File, int.Parse(text), int.Parse(text2), true);
                    Image.Src = Images.GetUncachedImageSrc(File);
                    Image.Visible = true;
                    Image.Width = Unit.Parse(text);
                    Image.Height = Unit.Parse(text2);
                    SheerResponse.SetAttribute("Image", "width", text);
                    SheerResponse.SetAttribute("Image", "height", text2);
                    HasFile.Disabled = false;
                    Context.ClientPage.Modified = false;
                }
                else
                {
                    Image.Src = Images.GetThemedImageSource("Images/ImageNotFound.gif");
                    Image.Visible = true;
                    Image.Width = new Unit(96.0, UnitType.Pixel);
                    Image.Height = new Unit(96.0, UnitType.Pixel);
                    SheerResponse.SetAttribute("Image", "width", text);
                    SheerResponse.SetAttribute("Image", "height", text2);
                    HasFile.Disabled = true;
                }
            }
            else
            {
                SheerResponse.Alert("This item is not associated with an image file.");
            }
        }

        /// <summary>
        /// Loads the image from the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        private void LoadImageFromFile(string file)
        {
            ItemID = string.Empty;
            UndoList = string.Empty;
            Work = -1;
            File = file;
            if (File.Length > 0)
            {
                using (Bitmap bitmap = new Bitmap(FileUtil.MapPath(file)))
                {
                    string src = Imager.CreateTemporaryFile(file);
                    AddUndo(src, bitmap.Width, bitmap.Height, true);
                    Image.Src = Images.GetUncachedImageSrc(file);
                    Image.Visible = true;
                    Image.Width = Unit.Parse(bitmap.Width.ToString());
                    Image.Height = Unit.Parse(bitmap.Height.ToString());
                    SheerResponse.SetAttribute("Image", "width", bitmap.Width.ToString());
                    SheerResponse.SetAttribute("Image", "height", bitmap.Height.ToString());
                    HasFile.Disabled = false;
                }
            }
        }

        /// <summary>
        /// Sets the image.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="undo">if set to <c>true</c> this instance is undo.</param>
        private void SetImage(string src, int width, int height, bool undo)
        {
            if (undo)
            {
                AddUndo(src, width, height, true);
            }
            src = Images.GetUncachedImageSrc(src);
            SheerResponse.SetAttribute("Image", "src", src);
            if (width > 0)
            {
                SheerResponse.SetAttribute("Image", "width", width.ToString());
            }
            if (height > 0)
            {
                SheerResponse.SetAttribute("Image", "height", height.ToString());
            }
        }

        /// <summary>
        /// Updates the specified SRC.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="undo">if set to <c>true</c> this instance is undo.</param>
        private void Update(string src, int width, int height, bool undo)
        {
            SheerResponse.Eval("rubberband.Hide()");
            SetImage(src, width, height, undo);
            Context.ClientPage.Modified = true;
        }

        /// <summary>
        /// Updates the ribbon.
        /// </summary>
        private void UpdateRibbon()
        {
            Ribbon ribbon = new Ribbon();
            ribbon.ID = "ImagerRibbon";
            Item item = null;
            if (!string.IsNullOrEmpty(ItemID))
            {
                item = Context.ContentDatabase.GetItem(ItemID);
            }
            ribbon.CommandContext = new CommandContext(item);
            ribbon.ShowContextualTabs = false;
            if (Work >= 0)
            {
                ribbon.CommandContext.Parameters["WorkFile"] = GetWorkFile(Work);
            }
            ribbon.CommandContext.Parameters["HasFile"] = (HasFile.Disabled ? "0" : "1");
            Item item2 = Context.Database.GetItem("/sitecore/content/Applications/Media/Imager/Ribbon");
            Error.AssertItemFound(item2, "/sitecore/content/Applications/Media/Imager/Ribbon");
            ribbon.CommandContext.RibbonSourceUri = item2.Uri;
            RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ribbon);
        }
    }
}