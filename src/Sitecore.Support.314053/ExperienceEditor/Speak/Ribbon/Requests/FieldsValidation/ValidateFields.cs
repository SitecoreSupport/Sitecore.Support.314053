using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Ribbon.Requests.FieldsValidation;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Switchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.FieldsValidation
{
  public class ValidateFields : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.FieldsValidation.ValidateFields
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      Item item = base.RequestContext.Item;
      Assert.IsNotNull(item, "Item is null");
      using (new ClientDatabaseSwitcher(item.Database))
      {
        // Use patched method to fix issue #314053
        ValidatorCollection fieldsValidators = ValidatorManager.GetFieldsValidators(
          ValidatorsMode.ValidatorBar,
          SaveArgsResolverProxy.GetControlsToValidate(base.RequestContext).Keys,
          item.Database);

        ValidatorManager.Validate(fieldsValidators, new ValidatorOptions(blocking: true));
        List<FieldValidationError> list = new List<FieldValidationError>();
        foreach (BaseValidator item2 in fieldsValidators)
        {
          if (!item2.IsValid && !(item2.FieldID == (ID)null))
          {
            if (Sitecore.ExperienceEditor.Utils.WebUtility.IsEditAllVersionsTicked())
            {
              Field field = item.Fields[item2.FieldID];
              if (!field.Shared && !field.Unversioned)
              {
                continue;
              }
            }
            list.Add(new FieldValidationError
            {
              Text = item2.Text,
              Title = item2.Name,
              FieldId = item2.FieldID.ToString(),
              DataSourceId = item2.ItemUri.ItemID.ToString(),
              Errors = item2.Errors,
              Priority = (int)item2.Result
            });
          }
        }

        PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
        pipelineProcessorResponseValue.Value = list;
        return pipelineProcessorResponseValue;
      }
    }
  }
}