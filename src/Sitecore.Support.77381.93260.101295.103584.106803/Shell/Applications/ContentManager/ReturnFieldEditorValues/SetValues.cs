using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Reflection;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentManager;
using Sitecore.Shell.Applications.ContentManager.ReturnFieldEditorValues;
using System;
using System.Web.UI;

namespace Sitecore.Support.Shell.Applications.ContentManager.ReturnFieldEditorValues
{
    public class SetValues
    {
        public void Process(ReturnFieldEditorValuesArgs args)
        {
            foreach (FieldInfo fieldInfo in args.FieldInfo.Values)
            {
                System.Web.UI.Control control = Context.ClientPage.FindSubControl(fieldInfo.ID);
                if (control != null)
                {
                    string text;
                    if (control is IContentField)
                    {
                        text = StringUtil.GetString(new string[]
						{
							(control as IContentField).GetValue()
						});
                    }
                    else
                    {
                        text = StringUtil.GetString(ReflectionUtil.GetProperty(control, "Value"));
                    }
                    if (!(text == "__#!$No value$!#__"))
                    {
                        string a = fieldInfo.Type.ToLowerInvariant();
                        if (a == "rich text" || a == "html")
                        {
                            text = text.TrimEnd(new char[]
							{
								' '
							});
                        }
                        if (a == "multi-line text")
                        {
                            text = text.Replace("\r\n", "<br/>").Replace("\n\r", "<br/>").Replace("\n", "<br/>").Replace("\r", "<br/>");
                        }
                        foreach (FieldDescriptor current in args.Options.Fields)
                        {
                            if (current.FieldID == fieldInfo.FieldID)
                            {
                                ItemUri uri = new ItemUri(fieldInfo.ItemID, fieldInfo.Language, fieldInfo.Version, Factory.GetDatabase(current.ItemUri.DatabaseName));
                                if (current.ItemUri == uri)
                                {
                                    current.Value = text;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}