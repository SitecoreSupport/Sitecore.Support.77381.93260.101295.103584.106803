using Newtonsoft.Json;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Pipelines.Save;
using Sitecore.Shell.Applications.WebEdit.Commands;
using System;
using System.Collections.Generic;

namespace Sitecore.Support.ExperienceEditor.Speak.Server.Contexts
{
    public class PageContext : ItemContext
    {
        [JsonProperty("scLayout")]
        public string LayoutSource
        {
            get;
            set;
        }

        [JsonProperty("scValidatorsKey")]
        public string ValidatorsKey
        {
            get;
            set;
        }

        [JsonProperty("scFieldValues")]
        public Dictionary<string, string> FieldValues
        {
            get;
            set;
        }

        public SaveArgs GetSaveArgs()
        {
            IEnumerable<PageEditorField> fields = Sitecore.Support.ExperienceEditor.Utils.WebUtility.GetFields(base.Item.Database, this.FieldValues);
            string empty = string.Empty;
            string layoutSource = this.LayoutSource;
            SaveArgs saveArgs = PipelineUtil.GenerateSaveArgs(base.Item, fields, empty, layoutSource, string.Empty, Sitecore.Support.ExperienceEditor.Utils.WebUtility.GetCurrentLayoutFieldId().ToString());
            saveArgs.HasSheerUI = false;
            ParseXml parseXml = new ParseXml();
            parseXml.Process(saveArgs);
            return saveArgs;
        }

        public SafeDictionary<FieldDescriptor, string> GetControlsToValidate()
        {
            Item item = base.Item;
            Assert.IsNotNull(item, "The item is null.");
            IEnumerable<PageEditorField> fields = Sitecore.Support.ExperienceEditor.Utils.WebUtility.GetFields(item.Database, this.FieldValues);
            SafeDictionary<FieldDescriptor, string> safeDictionary = new SafeDictionary<FieldDescriptor, string>();
            foreach (PageEditorField current in fields)
            {
                Item item2 = (item.ID == current.ItemID) ? item : item.Database.GetItem(current.ItemID);
                Field field = item.Fields[current.FieldID];
                string value = Sitecore.Support.ExperienceEditor.Utils.WebUtility.HandleFieldValue(current.Value, field.TypeKey);
                FieldDescriptor key = new FieldDescriptor(item2.Uri, field.ID, value, false);
                string text = current.ControlId ?? string.Empty;
                safeDictionary[key] = text;
                if (!string.IsNullOrEmpty(text))
                {
                    RuntimeValidationValues.Current[text] = value;
                }
            }
            return safeDictionary;
        }
    }
}