using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.UI
{
    /// <summary>解析结果：成功携带 Spec，失败携带错误列表。</summary>
    public class UISpecParseResult
    {
        public bool Ok;
        public UISpec Spec;
        public List<string> Errors = new List<string>();
    }

    /// <summary>JSON ↔ UISpec。反序列化用 Newtonsoft（工程已含 com.unity.nuget.newtonsoft-json，自动引用）。</summary>
    public static class UISpecJson
    {
        /// <summary>解析并校验。任一步失败返回 Ok=false 与错误列表。</summary>
        public static UISpecParseResult Parse(string json)
        {
            var result = new UISpecParseResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Errors.Add("JSON 为空");
                return result;
            }

            UISpec spec;
            try
            {
                spec = JsonConvert.DeserializeObject<UISpec>(json);
            }
            catch (Exception e)
            {
                result.Errors.Add($"JSON 反序列化失败: {e.Message}");
                return result;
            }

            if (spec == null)
            {
                result.Errors.Add("JSON 反序列化得到 null");
                return result;
            }

            var validationErrors = UISpecValidator.Validate(spec);
            if (validationErrors.Count > 0)
            {
                result.Errors.AddRange(validationErrors);
                return result;
            }

            result.Ok = true;
            result.Spec = spec;
            return result;
        }
    }
}
