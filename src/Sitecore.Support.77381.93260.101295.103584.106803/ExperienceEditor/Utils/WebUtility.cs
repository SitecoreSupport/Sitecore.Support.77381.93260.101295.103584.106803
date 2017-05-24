using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Exceptions;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.Configuration;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml;

namespace Sitecore.Support.ExperienceEditor.Utils
{
    public static class WebUtility
    {
        private static bool? isGetLayoutSourceFieldsExists;

        public static bool IsSublayoutInsertingMode
        {
            get
            {
                return !string.IsNullOrEmpty(WebUtil.GetQueryString("sc_ruid"));
            }
        }

        public static string ClientLanguage
        {
            get
            {
                return WebUtil.GetCookieValue("shell", "lang", Context.Language.Name);
            }
        }

        public static SiteInfo GetCurrentSiteInfo()
        {
            Assert.IsNotNull(Context.Request, "request");
            string name = string.IsNullOrEmpty(Context.Request.QueryString["sc_pagesite"]) ? Sitecore.Configuration.Settings.Preview.DefaultSite : Context.Request.QueryString["sc_pagesite"];
            return SiteContextFactory.GetSiteInfo(name);
        }

        public static bool IsLayoutPresetApplied()
        {
            return !WebUtility.IsSublayoutInsertingMode && !string.IsNullOrEmpty(Context.PageDesigner.PageDesignerHandle) && !string.IsNullOrEmpty(WebUtil.GetSessionString(Context.PageDesigner.PageDesignerHandle)) && !string.IsNullOrEmpty(WebUtil.GetSessionString(Context.PageDesigner.PageDesignerHandle + "_SAFE"));
        }

        public static string GetDevice(UrlString url)
        {
            Assert.ArgumentNotNull(url, "url");
            string result = string.Empty;
            DeviceItem device = Context.Device;
            if (device != null)
            {
                url["dev"] = device.ID.ToString();
                result = device.ID.ToShortID().ToString();
            }
            return Assert.ResultNotNull<string>(result);
        }

        public static void RenderLoadingIndicator(HtmlTextWriter output)
        {
            System.Web.UI.Page page = new System.Web.UI.Page();
            System.Web.UI.Control control = page.LoadControl("~/sitecore/shell/client/Sitecore/ExperienceEditor/PageEditbar/LoadingIndicator.ascx");
            control.RenderControl(output);
        }

        public static void RenderLayout(Item item, HtmlTextWriter output, string siteName, string deviceId)
        {
            string text = WebUtility.GetLayout(item);
            text = WebUtility.FixEmptyPlaceholders(text);
            text = WebUtility.ConvertToJson(text);
            output.Write("<input id=\"scLayout\" type=\"hidden\" value='" + text + "' />");
            output.Write("<input id=\"scDeviceID\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(deviceId) + "\" />");
            output.Write("<input id=\"scItemID\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(item.ID.ToShortID().ToString()) + "\" />");
            output.Write("<input id=\"scLanguage\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(item.Language.Name) + "\" />");
            output.Write("<input id=\"scSite\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(siteName) + "\" />");
        }

        private static string FixEmptyPlaceholders(string layout)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(layout);
            XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//r[@ph='']");
            if (xmlNodeList != null)
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    string value = xmlNode.Attributes["id"].Value;
                    Item item = Context.Database.GetItem(new ID(value));
                    string value2 = item.Fields[Sitecore.ExperienceEditor.Constants.FieldNames.Placeholder].Value;
                    if (!string.IsNullOrEmpty(value2))
                    {
                        xmlNode.Attributes["ph"].Value = value2;
                    }
                }
            }
            layout = xmlDocument.OuterXml;
            return layout;
        }

        public static string ConvertToJson(string layout)
        {
            Assert.ArgumentNotNull(layout, "layout");
            string result = WebEditUtil.ConvertXMLLayoutToJSON(layout);
            return Assert.ResultNotNull<string>(result);
        }

        public static string GetLayout(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            LayoutField layoutField = new LayoutField(item);
            return WebUtility.GetLayout(layoutField);
        }

        public static string GetLayout(Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            LayoutField layoutField = new LayoutField(field);
            return WebUtility.GetLayout(layoutField);
        }

        public static string GetLayout(LayoutField layoutField)
        {
            Assert.ArgumentNotNull(layoutField, "field");
            string result = layoutField.Value;
            if (!Context.PageDesigner.IsDesigning)
            {
                return Assert.ResultNotNull<string>(result);
            }
            string pageDesignerHandle = Context.PageDesigner.PageDesignerHandle;
            if (string.IsNullOrEmpty(pageDesignerHandle))
            {
                return Assert.ResultNotNull<string>(result);
            }
            string sessionString = WebUtil.GetSessionString(pageDesignerHandle);
            if (!string.IsNullOrEmpty(sessionString))
            {
                result = sessionString;
            }
            return Assert.ResultNotNull<string>(result);
        }

        public static System.Collections.Generic.Dictionary<string, string> ConvertFormKeysToDictionary(NameValueCollection form)
        {
            Assert.ArgumentNotNull(form, "dictionaryForm");
            return (from string key in form.Keys
                    where !string.IsNullOrEmpty(key)
                    select key).ToDictionary((string key) => key, (string key) => form[key]);
        }

        public static System.Collections.Generic.IEnumerable<PageEditorField> GetFields(Database database, System.Collections.Generic.Dictionary<string, string> dictionaryForm)
        {
            Assert.ArgumentNotNull(dictionaryForm, "dictionaryForm");
            System.Collections.Generic.List<PageEditorField> list = new System.Collections.Generic.List<PageEditorField>();
            foreach (string current in dictionaryForm.Keys)
            {
                if (current.StartsWith("fld_", System.StringComparison.InvariantCulture) || current.StartsWith("flds_", System.StringComparison.InvariantCulture))
                {
                    string text = current;
                    string text2 = dictionaryForm[current];
                    int num = text.IndexOf('$');
                    if (num >= 0)
                    {
                        text = StringUtil.Left(text, num);
                    }
                    string[] array = text.Split(new char[]
					{
						'_'
					});
                    ID iD = ShortID.DecodeID(array[1]);
                    ID fieldID = ShortID.DecodeID(array[2]);
                    Language language = Language.Parse(array[3]);
                    Sitecore.Data.Version version = Sitecore.Data.Version.Parse(array[4]);
                    string revision = array[5];
                    Item item = database.GetItem(iD);
                    if (item != null)
                    {
                        Field field = item.Fields[fieldID];
                        if (current.StartsWith("flds_", System.StringComparison.InvariantCulture))
                        {
                            text2 = (string)WebUtil.GetSessionValue(text2);
                            if (string.IsNullOrEmpty(text2))
                            {
                                text2 = field.Value;
                            }
                        }
                        string typeKey;
                        if ((typeKey = field.TypeKey) != null)
                        {
                            if (!(typeKey == "html") && !(typeKey == "rich text"))
                            {
                                if (!(typeKey == "text"))
                                {
                                    if (typeKey == "multi-line text" || typeKey == "memo")
                                    {
                                        // Regex regex = new Regex("<br.*/*>", RegexOptions.IgnoreCase);
                                        // text2 = regex.Replace(text2, "\r\n");
                                        // Begin of Sitecore.Support.101295
                                        Regex regex = new Regex("<br.*?/*?>", RegexOptions.IgnoreCase);
                                        text2 = regex.Replace(text2, "\r\n");
                                        // End of Sitecore.Support.101295
                                        text2 = StringUtil.RemoveTags(text2);
                                    }
                                }
                                else
                                {
                                    text2 = StringUtil.RemoveTags(text2);
                                }
                            }
                            else
                            {
                                text2 = text2.TrimEnd(new char[]
								{
									' '
								});
                            }
                        }
                        PageEditorField item2 = new PageEditorField
                        {
                            ControlId = text,
                            FieldID = fieldID,
                            ItemID = iD,
                            Language = language,
                            Revision = revision,
                            Value = text2,
                            Version = version
                        };
                        list.Add(item2);
                    }
                }
            }
            return list;
        }

        public static System.Collections.Generic.IEnumerable<PageEditorField> GetFields(Item item)
        {
            System.Collections.Generic.List<PageEditorField> list = new System.Collections.Generic.List<PageEditorField>();
            foreach (Field field in item.Fields)
            {
                PageEditorField item2 = new PageEditorField
                {
                    ControlId = null,
                    FieldID = field.ID,
                    ItemID = field.Item.ID,
                    Language = field.Language,
                    Revision = item[FieldIDs.Revision],
                    Value = field.Value,
                    Version = item.Version
                };
                list.Add(item2);
            }
            return list;
        }

        public static Packet CreatePacket(Database database, System.Collections.Generic.IEnumerable<PageEditorField> fields, out SafeDictionary<FieldDescriptor, string> controlsToValidate)
        {
            Assert.ArgumentNotNull(fields, "fields");
            Packet packet = new Packet();
            controlsToValidate = new SafeDictionary<FieldDescriptor, string>();
            foreach (PageEditorField current in fields)
            {
                FieldDescriptor fieldDescriptor = WebUtility.AddField(database, packet, current);
                if (fieldDescriptor != null)
                {
                    string text = current.ControlId ?? string.Empty;
                    controlsToValidate[fieldDescriptor] = text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        RuntimeValidationValues.Current[text] = fieldDescriptor.Value;
                    }
                }
            }
            return packet;
        }

        public static FieldDescriptor AddField(Database database, Packet packet, PageEditorField pageEditorField)
        {
            Assert.ArgumentNotNull(packet, "packet");
            Assert.ArgumentNotNull(pageEditorField, "pageEditorField");
            Item item = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
            if (item == null)
            {
                return null;
            }
            Field field = item.Fields[pageEditorField.FieldID];
            string text = WebUtility.HandleFieldValue(pageEditorField.Value, field.TypeKey);
            string fieldValidationErrorMessage = WebUtility.GetFieldValidationErrorMessage(field, text);
            if (fieldValidationErrorMessage != string.Empty)
            {
                throw new FieldValidationException(fieldValidationErrorMessage, field);
            }
            if (!(text == field.Value))
            {
                XmlNode xmlNode = packet.XmlDocument.SelectSingleNode(string.Concat(new object[]
				{
					"/*/field[@itemid='",
					pageEditorField.ItemID,
					"' and @language='",
					pageEditorField.Language,
					"' and @version='",
					pageEditorField.Version,
					"' and @fieldid='",
					pageEditorField.FieldID,
					"']"
				}));
                if (xmlNode != null)
                {
                    Item item2 = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
                    if (item2 == null)
                    {
                        return null;
                    }
                    if (text != item2[pageEditorField.FieldID])
                    {
                        xmlNode.ChildNodes[0].InnerText = text;
                    }
                }
                else
                {
                    packet.StartElement("field");
                    packet.SetAttribute("itemid", pageEditorField.ItemID.ToString());
                    packet.SetAttribute("language", pageEditorField.Language.ToString());
                    packet.SetAttribute("version", pageEditorField.Version.ToString());
                    packet.SetAttribute("fieldid", pageEditorField.FieldID.ToString());
                    packet.SetAttribute("itemrevision", pageEditorField.Revision);
                    packet.AddElement("value", text, new string[0]);
                    packet.EndElement();
                }
                return new FieldDescriptor(item.Uri, field.ID, text, false);
            }
            string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(field, text);
            if (string.IsNullOrEmpty(fieldRegexValidationError))
            {
                return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
            }
            if (item.Paths.IsMasterPart || StandardValuesManager.IsStandardValuesHolder(item))
            {
                return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
            }
            throw new FieldValidationException(fieldRegexValidationError, field);
        }

        public static string HandleFieldValue(string value, string fieldTypeKey)
        {
            switch (fieldTypeKey)
            {
                case "html":
                case "rich text":
                    value = value.TrimEnd(new char[]
				{
					' '
				});
                    value = WebEditUtil.RepairLinks(value);
                    break;
                case "text":
                case "single-line text":
                    value = HttpUtility.HtmlDecode(value);
                    break;
                case "integer":
                case "number":
                    value = StringUtil.RemoveTags(value);
                    break;
                case "multi-line text":
                case "memo":
                    {
                        // Regex regex = new Regex("<br.*/*>", RegexOptions.IgnoreCase);
                        // value = regex.Replace(value, "\r\n");
                        // Begin of Sitecore.Support.101295
                        Regex regex = new Regex("<br.*?/*?>", RegexOptions.IgnoreCase);
                        value = regex.Replace(value, "\r\n");
                        // End of Sitecore.Support.101295
                        value = StringUtil.RemoveTags(value);
                        break;
                    }
                case "word document":
                    value = string.Join(System.Environment.NewLine, value.Split(new string[]
				{
					"\r\n",
					"\n\r",
					"\n"
				}, System.StringSplitOptions.None));
                    break;
            }
            return value;
        }

        public static string GetFieldValidationErrorMessage(Field field, string value)
        {
            Assert.ArgumentNotNull(field, "field");
            Assert.ArgumentNotNull(value, "value");
            if (!Sitecore.Configuration.Settings.WebEdit.ValidationEnabled)
            {
                return string.Empty;
            }
            System.Globalization.CultureInfo cultureInfo = LanguageUtil.GetCultureInfo();
            if (value.Length == 0)
            {
                return string.Empty;
            }
            string typeKey;
            if ((typeKey = field.TypeKey) != null)
            {
                if (!(typeKey == "integer"))
                {
                    if (typeKey == "number")
                    {
                        double num;
                        if (double.TryParse(value, System.Globalization.NumberStyles.Float, cultureInfo, out num))
                        {
                            return string.Empty;
                        }
                        return Translate.Text("\"{0}\" is not a valid number.", new object[]
						{
							value
						});
                    }
                }
                else
                {
                    long num2;
                    if (long.TryParse(value, System.Globalization.NumberStyles.Integer, cultureInfo, out num2))
                    {
                        return string.Empty;
                    }
                    return Translate.Text("\"{0}\" is not a valid integer.", new object[]
					{
						value
					});
                }
            }
            return string.Empty;
        }

        public static void AddLayoutField(string layout, Packet packet, Item item, string fieldId = null)
        {
            Assert.ArgumentNotNull(packet, "packet");
            Assert.ArgumentNotNull(item, "item");
            if (fieldId == null)
            {
                fieldId = FieldIDs.FinalLayoutField.ToString();
            }
            if (string.IsNullOrEmpty(layout))
            {
                return;
            }
            layout = WebEditUtil.ConvertJSONLayoutToXML(layout);
            Assert.IsNotNull(layout, layout);
            if (!WebUtility.IsEditAllVersionsTicked())
            {
                layout = XmlDeltas.GetDelta(layout, new LayoutField(item.Fields[FieldIDs.LayoutField]).Value);
            }
            packet.StartElement("field");
            packet.SetAttribute("itemid", item.ID.ToString());
            packet.SetAttribute("language", item.Language.ToString());
            packet.SetAttribute("version", item.Version.ToString());
            packet.SetAttribute("fieldid", fieldId);
            packet.AddElement("value", layout, new string[0]);
            packet.EndElement();
        }

        public static UrlString BuildChangeLanguageUrl(UrlString url, ItemUri itemUri, string languageName)
        {
            UrlString urlString = new UrlString(url.GetUrl());
            if (itemUri == null)
            {
                return null;
            }
            SiteContext site = SiteContext.GetSite(WebEditUtil.SiteName);
            if (site == null)
            {
                return null;
            }
            Item itemNotNull = Client.GetItemNotNull(itemUri);
            using (new SiteContextSwitcher(site))
            {
                using (new LanguageSwitcher(itemNotNull.Language))
                {
                    urlString = WebUtility.BuildChangeLanguageNewUrl(languageName, url, itemNotNull);
                    LanguageEmbedding languageEmbedding = LinkManager.LanguageEmbedding;
                    if (languageEmbedding == LanguageEmbedding.Never)
                    {
                        urlString["sc_lang"] = languageName;
                    }
                    else
                    {
                        urlString.Remove("sc_lang");
                    }
                }
            }
            return urlString;
        }

        public static string GetContentEditorDialogFeatures()
        {
            string text = "location=0,menubar=0,status=0,toolbar=0,resizable=1,getBestDialogSize:true";
            DeviceItem device = Context.Device;
            if (device == null)
            {
                return text;
            }
            SitecoreClientDeviceCapabilities sitecoreClientDeviceCapabilities = device.Capabilities as SitecoreClientDeviceCapabilities;
            if (sitecoreClientDeviceCapabilities == null)
            {
                return text;
            }
            if (sitecoreClientDeviceCapabilities.RequiresScrollbarsOnWindowOpen)
            {
                text += ",scrollbars=1,dependent=1";
            }
            return text;
        }

        public static bool IsQueryStateEnabled<T>(Item contextItem) where T : Command, new()
        {
            T t = System.Activator.CreateInstance<T>();
            CommandContext context = new CommandContext(new Item[]
			{
				contextItem
			});
            return t.QueryState(context) == CommandState.Enabled;
        }

        public static bool IsEditAllVersionsTicked()
        {
            return StringUtility.EvaluateCheckboxRegistryKeyValue(Registry.GetString(Sitecore.ExperienceEditor.Constants.RegistryKeys.EditAllVersions)) && WebUtility.IsEditAllVersionsAllowed();
        }

        public static bool IsEditAllVersionsAllowed()
        {
            if (!WebUtility.isGetLayoutSourceFieldsExists.HasValue)
            {
                WebUtility.isGetLayoutSourceFieldsExists = new bool?(CorePipelineFactory.GetPipeline("getLayoutSourceFields", string.Empty) != null);
                if (!WebUtility.isGetLayoutSourceFieldsExists.Value)
                {
                    Log.Warn("Pipeline getLayoutSourceFields is turned off.", new object());
                }
            }
            return Context.Site != null && WebUtility.isGetLayoutSourceFieldsExists.Value && Sitecore.ExperienceEditor.Settings.WebEdit.ExperienceEditorEditAllVersions && Context.Site.DisplayMode != DisplayMode.Normal && WebUtil.GetQueryString("sc_disable_edit") != "yes" && WebUtil.GetQueryString("sc_duration") != "temporary";
        }

        public static ID GetCurrentLayoutFieldId()
        {
            if (!WebUtility.IsEditAllVersionsTicked())
            {
                return FieldIDs.FinalLayoutField;
            }
            return FieldIDs.LayoutField;
        }

        private static UrlString BuildChangeLanguageNewUrl(string languageName, UrlString url, Item item)
        {
            Assert.ArgumentNotNull(languageName, "languageName");
            Assert.ArgumentNotNull(url, "url");
            Assert.ArgumentNotNull(item, "item");
            Language language;
            bool condition = Language.TryParse(languageName, out language);
            Assert.IsTrue(condition, string.Format("Cannot parse the language ({0}).", languageName));
            UrlOptions defaultOptions = UrlOptions.DefaultOptions;
            defaultOptions.Language = language;
            Item item2 = item.Database.GetItem(item.ID, language);
            Assert.IsNotNull(item2, string.Format("Item not found ({0}, {1}).", item.ID, language));
            string itemUrl = LinkManager.GetItemUrl(item2, defaultOptions);
            UrlString urlString = new UrlString(itemUrl);
            foreach (string name in url.Parameters.Keys)
            {
                urlString.Parameters[name] = url.Parameters[name];
            }
            return urlString;
        }
    }
}