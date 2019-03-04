using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Exceptions;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.Pipelines.Save;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace Sitecore.Support
{
  public class SaveArgsResolverProxy
  {
    public static SafeDictionary<FieldDescriptor, string> GetControlsToValidate(PageContext context)
    {
      Item item = context.Item;
      Assert.IsNotNull(item, "The item is null.");

      IEnumerable<PageEditorField> fields = Sitecore.ExperienceEditor.Utils.WebUtility.GetFields(item.Database, context.FieldValues);
      SafeDictionary<FieldDescriptor, string> safeDictionary = new SafeDictionary<FieldDescriptor, string>();
      foreach (PageEditorField item3 in fields)
      {
        Item item2 = (item.ID == item3.ItemID) ? item : item.Database.GetItem(item3.ItemID);
        Field field = item.Fields[item3.FieldID];

        // Use patched method
        string value = HandleFieldValue(item3.Value, field.TypeKey);

        FieldDescriptor key = new FieldDescriptor(item2.Uri, field.ID, value, containsStandardValue: false);
        string text2 = safeDictionary[key] = (item3.ControlId ?? string.Empty);
        if (!string.IsNullOrEmpty(text2))
        {
          RuntimeValidationValues.Current[text2] = value;
        }
      }
      return safeDictionary;
    }

    public static SaveArgs GetSaveArgs(PageContext context)
    {
      IEnumerable<PageEditorField> fields = Sitecore.ExperienceEditor.Utils.WebUtility.GetFields(context.Item.Database, context.FieldValues);
      string empty = string.Empty;
      string layoutSource = context.LayoutSource;

      // Use patched method to fix issue #314053
      SaveArgs saveArgs = GenerateSaveArgs(
        context.Item,
        fields,
        empty, 
        layoutSource,
        string.Empty,
        Sitecore.ExperienceEditor.Utils.WebUtility.GetCurrentLayoutFieldId().ToString());

      saveArgs.HasSheerUI = false;
      ParseXml parseXml = new ParseXml();
      parseXml.Process(saveArgs);
      return saveArgs;
    }

    public static SaveArgs GenerateSaveArgs(Item contextItem, IEnumerable<PageEditorField> fields, string postAction, string layoutValue, string validatorsKey, string fieldId = null)
    {
      SafeDictionary<FieldDescriptor, string> controlsToValidate;

      // Use patched method to fix issue #314053
      Packet packet = CreatePacket(contextItem.Database, fields, out controlsToValidate);

      if (Sitecore.Web.WebEditUtil.CanDesignItem(contextItem))
      {
        Sitecore.ExperienceEditor.Utils.WebUtility.AddLayoutField(layoutValue, packet, contextItem, fieldId);
      }
      if (!string.IsNullOrEmpty(validatorsKey))
      {
        ValidatorsMode mode;
        ValidatorCollection validators = Sitecore.ExperienceEditor.Utils.PipelineUtil.GetValidators(contextItem, controlsToValidate, out mode);
        validators.Key = validatorsKey;
        ValidatorManager.SetValidators(mode, validatorsKey, validators);
      }
      SaveArgs saveArgs = new SaveArgs(packet.XmlDocument);
      saveArgs.SaveAnimation = false;
      saveArgs.PostAction = postAction;
      saveArgs.PolicyBasedLocking = true;
      return saveArgs;
    }

    public static Packet CreatePacket(Database database, IEnumerable<PageEditorField> fields, out SafeDictionary<FieldDescriptor, string> controlsToValidate)
    {
      Assert.ArgumentNotNull(fields, "fields");
      Packet packet = new Packet();
      controlsToValidate = new SafeDictionary<FieldDescriptor, string>();
      foreach (PageEditorField field in fields)
      {
        // Use patched method to fix issue #314053
        FieldDescriptor fieldDescriptor = AddField(database, packet, field);
        if (fieldDescriptor != null)
        {
          string text = field.ControlId ?? string.Empty;
          controlsToValidate[fieldDescriptor] = text;
          if (!string.IsNullOrEmpty(text))
          {
            RuntimeValidationValues.Current[text] = fieldDescriptor.Value;
          }
        }
      }
      return packet;
    }

    public static FieldDescriptor AddField(Database database, Packet packet, PageEditorField pageEditorField)
    {
      Assert.ArgumentNotNull(packet, "packet");
      Assert.ArgumentNotNull(pageEditorField, "pageEditorField");
      Item item = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
      if (item == null)
      {
        return null;
      }
      Field field = item.Fields[pageEditorField.FieldID];

      // Use patched method to fix issue #314053
      string text = HandleFieldValue(pageEditorField.Value, field.TypeKey);

      string fieldValidationErrorMessage = Sitecore.ExperienceEditor.Utils.WebUtility.GetFieldValidationErrorMessage(field, text);
      if (fieldValidationErrorMessage != string.Empty)
      {
        throw new FieldValidationException(fieldValidationErrorMessage, field);
      }
      if (text == field.Value)
      {
        string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(field, text);
        if (!string.IsNullOrEmpty(fieldRegexValidationError))
        {
          if (item.Paths.IsMasterPart || StandardValuesManager.IsStandardValuesHolder(item))
          {
            return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
          }
          throw new FieldValidationException(fieldRegexValidationError, field);
        }
        return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
      }
      XmlNode xmlNode = packet.XmlDocument.SelectSingleNode("/*/field[@itemid='" + pageEditorField.ItemID + "' and @language='" + pageEditorField.Language + "' and @version='" + pageEditorField.Version + "' and @fieldid='" + pageEditorField.FieldID + "']");
      if (xmlNode != null)
      {
        Item item2 = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
        if (item2 == null)
        {
          return null;
        }
        if (text != item2[pageEditorField.FieldID])
        {
          xmlNode.ChildNodes[0].InnerText = text;
        }
      }
      else
      {
        packet.StartElement("field");
        packet.SetAttribute("itemid", pageEditorField.ItemID.ToString());
        packet.SetAttribute("language", pageEditorField.Language.ToString());
        packet.SetAttribute("version", pageEditorField.Version.ToString());
        packet.SetAttribute("fieldid", pageEditorField.FieldID.ToString());
        packet.SetAttribute("itemrevision", pageEditorField.Revision);
        packet.AddElement("value", text);
        packet.EndElement();
      }
      return new FieldDescriptor(item.Uri, field.ID, text, containsStandardValue: false);
    }

    public static string HandleFieldValue(string value, string fieldTypeKey)
    {
      switch (fieldTypeKey)
      {
        case "html":
        case "rich text":
          value = value.TrimEnd(' ');

          // Use patched method to fix issue #314053
          value = Sitecore.Support.Web.WebEditUtil.RepairLinks(value);
          break;
        case "text":
        case "single-line text":
          value = HttpUtility.HtmlDecode(value);
          break;
        case "integer":
        case "number":
          value = StringUtil.RemoveTags(value);
          break;
        case "multi-line text":
        case "memo":
          {
            Regex regex = new Regex("<br.*/*>", RegexOptions.IgnoreCase);
            value = regex.Replace(value, "\r\n");
            value = StringUtil.RemoveTags(value);
            break;
          }
        case "word document":
          value = string.Join(Environment.NewLine, value.Split(new string[3]
          {
            "\r\n",
            "\n\r",
            "\n"
          }, StringSplitOptions.None));
          break;
      }
      return value;
    }

  }
}