using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Switchers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
    public class CallServerSavePipeline : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.CallServerSavePipeline
    {
        // Methods
        public override PipelineProcessorResponseValue ProcessRequest()
        {
            this.FixValues(base.RequestContext.Item.Database, base.RequestContext.FieldValues);

            PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
            Sitecore.Pipelines.Pipeline pipeline = Sitecore.Pipelines.PipelineFactory.GetPipeline("saveUI");
            pipeline.ID = Sitecore.Data.ShortID.Encode(Sitecore.Data.ID.NewID);
            Sitecore.Pipelines.Save.SaveArgs saveArgs = base.RequestContext.GetSaveArgs();
            PipelineProcessorResponseValue result;
            using (new ClientDatabaseSwitcher(base.RequestContext.Item.Database))
            {
                pipeline.Start(saveArgs);
                Sitecore.Caching.CacheManager.GetItemCache(base.RequestContext.Item.Database).Clear();
                pipelineProcessorResponseValue.AbortMessage = Sitecore.Globalization.Translate.Text(saveArgs.Error);
                result = pipelineProcessorResponseValue;
            }
            return result;
        }

        private void FixValues(Database database, Dictionary<string, string> dictionaryForm)
        {
            foreach (string str in dictionaryForm.Keys.ToArray<string>())
            {
                if (str.StartsWith("fld_", StringComparison.InvariantCulture) || str.StartsWith("flds_", StringComparison.InvariantCulture))
                {
                    string text = str;
                    int index = text.IndexOf('$');
                    if (index >= 0)
                    {
                        text = StringUtil.Left(text, index);
                    }
                    string[] strArray2 = text.Split(new char[] { '_' });
                    ID itemId = ShortID.DecodeID(strArray2[1]);
                    ID id2 = ShortID.DecodeID(strArray2[2]);
                    Item item = database.GetItem(itemId);
                    if (item != null)
                    {
                        Field field = item.Fields[id2];
                        string typeKey = field.TypeKey;
                        if ((typeKey != null) && typeKey.Equals("single-line text", StringComparison.InvariantCultureIgnoreCase))
                        {
                            dictionaryForm[str] = StringUtil.RemoveTags(dictionaryForm[str]);
                        }
                    }
                }
            }
        }
    }


}