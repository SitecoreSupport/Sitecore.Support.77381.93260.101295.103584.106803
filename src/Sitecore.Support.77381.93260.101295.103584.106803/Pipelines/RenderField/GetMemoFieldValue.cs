using Sitecore.Pipelines.RenderField;

namespace Sitecore.Support.Pipelines.RenderField
{
    public class GetMemoFieldValue
    {
        // Methods
        public void Process(RenderFieldArgs args)
        {
            switch (args.FieldTypeKey)
            {
                case "memo":
                case "multi-line text":
                    {
                        string linebreaks = args.RenderParameters["line-breaks"] ?? args.RenderParameters["linebreaks"];
                        if (linebreaks == null)
                        {
                            linebreaks = "<br/>";
                        }
                        args.Result.FirstPart = Replace(args.Result.FirstPart, linebreaks);
                        args.Result.LastPart = Replace(args.Result.LastPart, linebreaks);
                        break;
                    }
            }
        }

        private static string Replace(string output, string linebreaks)
        {
            output = output.Replace("\r\r\n", linebreaks);
            output = output.Replace("\r\n", linebreaks);
            output = output.Replace("\n\r", linebreaks);
            output = output.Replace("\n", linebreaks);
            output = output.Replace("\r", linebreaks);
            return output;
        }

    }
}