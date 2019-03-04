using Sitecore.Caching;
using Sitecore.Data;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Switchers;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.Pipelines.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CallServerSavePipeline : Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem.CallServerSavePipeline
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Pipeline pipeline = PipelineFactory.GetPipeline("saveUI");
      pipeline.ID = ShortID.Encode(ID.NewID);

      // Use patched method to fix issue #314053
      SaveArgs saveArgs = SaveArgsResolverProxy.GetSaveArgs(base.RequestContext);

      using (new ClientDatabaseSwitcher(base.RequestContext.Item.Database))
      {
        pipeline.Start(saveArgs);
        CacheManager.GetItemCache(base.RequestContext.Item.Database).Clear();
        pipelineProcessorResponseValue.AbortMessage = Translate.Text(saveArgs.Error);
        return pipelineProcessorResponseValue;
      }
    }
  }
}