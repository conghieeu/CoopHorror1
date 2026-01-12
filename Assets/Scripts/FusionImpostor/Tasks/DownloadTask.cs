using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionImpostor
{
    /// <summary>
    /// Simple task that simulates downloading a file in game.
    /// </summary>
    public class DownloadTask : TaskBase
    {
        public override string Name => "Download Files";

        public Image downloadFill;
        public Button downloadButton;
        public TMP_Text downloadPercentText;

        public int downloadTime = 5;
        private float amountDownloaded = 0;

        public void BeginDownload()
        {
            downloadButton.interactable = false;
            StartCoroutine(downloadRoutine());
        }

        public IEnumerator downloadRoutine()
        {
            while(amountDownloaded < downloadTime)
            {
                amountDownloaded += Time.deltaTime;
                downloadFill.fillAmount = (amountDownloaded / downloadTime);
                downloadPercentText.text = $"{downloadFill.fillAmount * 100:0}%";
                yield return null;
            }
            downloadPercentText.text = "Download Complete!";
            yield return new WaitForSeconds(1);
            Completed();
        }

        public override void ResetTask()
        {
            amountDownloaded = 0;
            downloadFill.fillAmount = 0;
            downloadPercentText.text = "0%";
            downloadButton.interactable = true;
        }
    }
}