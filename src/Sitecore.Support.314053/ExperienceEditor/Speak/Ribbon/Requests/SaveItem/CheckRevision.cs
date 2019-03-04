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
  public class CheckRevision : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.CheckRevision
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      // Use patched method
      SaveArgs.SaveItem saveItem = SaveArgsResolverProxy.GetSaveArgs(base.RequestContext).Items[0];

      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();

      // Merge with patch for issue #76398
      Item item = base.RequestContext.Item.Database.GetItem(saveItem.ID, saveItem.Language, saveItem.Version);
      if (item == null)
      {
        return pipelineProcessorResponseValue;
      }
      string text = item[FieldIDs.Revision].Replace("-", string.Empty);
      if (saveItem.Revision == string.Empty)
      {
        saveItem.Revision = text;
      }

      string strB = saveItem.Revision.Replace("-", string.Empty);
      if (string.Compare(text, strB, StringComparison.InvariantCultureIgnoreCase) != 0 && string.Compare("#!#Ignore revision#!#", strB, StringComparison.InvariantCultureIgnoreCase) != 0)
      {
        pipelineProcessorResponseValue.ConfirmMessage = Translate.Text("One or more items have been changed.\n\nDo you want to overwrite these changes?");
      }
      return pipelineProcessorResponseValue;
    }
  }
}