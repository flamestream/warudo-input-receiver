using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Flamestream.Props.Switch_Pro_Controller
{
    public class AnimSettingsReader : MonoBehaviour
    {
        public TextAsset jsonFile;

        public Dictionary<string, object> get()
        {
            if (jsonFile == null || string.IsNullOrEmpty(jsonFile.text))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonFile.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to parse JSON from {gameObject.name}: {ex.Message}");
                return null;
            }
        }

        public string getRawJson()
        {
            return jsonFile != null ? jsonFile.text : string.Empty;
        }
    }
}
