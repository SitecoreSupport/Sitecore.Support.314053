using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.ExperienceEditor.Utils;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class Validators : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.Validators
  {
    protected override IEnumerable<BaseValidator> GetValidators(Item item)
    {
      // Use patched method to fix issue #314053
      SafeDictionary<FieldDescriptor, string> controlsToValidate = SaveArgsResolverProxy.GetControlsToValidate(base.RequestContext);

      ValidatorsMode mode;
      ValidatorCollection validators = PipelineUtil.GetValidators(item, controlsToValidate, out mode);
      validators.Key = base.RequestContext.ValidatorsKey;
      return validators;
    }
  }
}