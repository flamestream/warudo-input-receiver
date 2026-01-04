using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Flamestream.Props.Switch_Pro_Controller
{
    public class AnimSettingsReader : MonoBehaviour
    {
        public TextAsset jsonFile;

        public string getRawJson()
        {
            return jsonFile != null ? jsonFile.text : string.Empty;
        }
    }
}
