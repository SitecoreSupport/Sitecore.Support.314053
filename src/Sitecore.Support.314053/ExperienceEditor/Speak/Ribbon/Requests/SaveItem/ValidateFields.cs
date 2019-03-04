using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class ValidateFields : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.ValidateFields
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();

      // Use patched method to fix issue #314053
      SaveArgs.SaveItem saveItem = SaveArgsResolverProxy.GetSaveArgs(base.RequestContext).Items[0];

      Item item = base.RequestContext.Item.Database.GetItem(saveItem.ID, saveItem.Language);
      if (item == null || item.Paths.IsMasterPart || StandardValuesManager.IsStandardValuesHolder(item))
      {
        return pipelineProcessorResponseValue;
      }
      SaveArgs.SaveField[] fields = saveItem.Fields;
      foreach (SaveArgs.SaveField saveField in fields)
      {
        Field field = item.Fields[saveField.ID];
        string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(field, saveField.Value);
        if (!string.IsNullOrEmpty(fieldRegexValidationError))
        {
          pipelineProcessorResponseValue.AbortMessage = Translate.Text(fieldRegexValidationError);
          break;
        }
      }
      return pipelineProcessorResponseValue;
    }
  
  }
}