using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CheckTemplateFieldChange : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.CheckTemplateFieldChange
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Item item = base.RequestContext.Item.Database.GetItem(base.RequestContext.ItemId, Language.Parse(base.RequestContext.Language), Sitecore.Data.Version.Parse(base.RequestContext.Version));
      if (item == null || item.TemplateID != TemplateIDs.TemplateField)
      {
        return pipelineProcessorResponseValue;
      }

      // Use patched method to fix issue #314053
      SaveArgs.SaveItem saveItem = SaveArgsResolverProxy.GetSaveArgs(base.RequestContext).Items[0];

      SaveArgs.SaveField field = GetField(saveItem, TemplateFieldIDs.Shared);
      SaveArgs.SaveField field2 = GetField(saveItem, TemplateFieldIDs.Unversioned);
      Field field3 = item.Fields[TemplateFieldIDs.Shared];
      Field field4 = item.Fields[TemplateFieldIDs.Unversioned];
      bool flag = false;
      if (field != null && field3.Value != field.Value)
      {
        flag = true;
      }
      if (field2 != null && field4.Value != field2.Value)
      {
        flag = true;
      }
      if (flag)
      {
        StringBuilder stringBuilder = new StringBuilder(Translate.Text("You have changed the unversioned or shared flag.\n\n"));
        stringBuilder.Append(Translate.Text("Enabling either of these flags will initiate a background process\nto update all the items based on this template.\n\nThis process might demand a lot of resources.\n\nIf you have enabled either the unversioned or shared flag, the previous version values of these\nfields will be lost.\n\nAre you sure you want to proceed?"));
        pipelineProcessorResponseValue.ConfirmMessage = stringBuilder.ToString();
      }
      return pipelineProcessorResponseValue;
    }
  }
}