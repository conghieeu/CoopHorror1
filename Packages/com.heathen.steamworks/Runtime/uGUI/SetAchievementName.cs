#if !DISABLESTEAMWORKS  && STEAMWORKSNET
using UnityEngine;

namespace Heathen.SteamworksIntegration.UI
{
    [RequireComponent(typeof(TMPro.TextMeshProUGUI))]
    public class SetAchievementName : MonoBehaviour
    {
        public string apiName;
        private TMPro.TextMeshProUGUI displayName;

        private void Start()
        {
            displayName = GetComponent<TMPro.TextMeshProUGUI>();

            if (!string.IsNullOrEmpty(apiName))
            {
                if (API.App.Initialized)
                    displayName.text = API.StatsAndAchievements.Client.GetAchievementDisplayAttribute(apiName, AchievementAttributes.name);
                else
                    API.App.evtSteamInitialized.AddListener(Refresh);
            }
        }

        public void Refresh()
        {
            if (!string.IsNullOrEmpty(apiName))
                displayName.text = API.StatsAndAchievements.Client.GetAchievementDisplayAttribute(apiName, AchievementAttributes.name);

            API.App.evtSteamInitialized.RemoveListener(Refresh);
        }
    }
}
#endif