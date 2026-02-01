using TMPro;
using UnityEngine;

// hiển thị tỷ lệ mua của công ty trong giao diện người dùng.
public class DisplayCompanyBuyingRate : MonoBehaviour
{
	public TextMeshProUGUI displayText;

	private void Update()
	{
		// displayText.text = $"{Mathf.RoundToInt(StartOfRound.Instance.companyBuyingRate * 100f)}%";
	}
}
