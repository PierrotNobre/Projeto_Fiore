using UnityEngine;
using TMPro;

public class TravelProgressUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject cityPanel;

    [SerializeField]
    private GameObject travelPanel;

    [SerializeField]
    private GameObject travelProgressPanel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text routeText;

    [SerializeField]
    private TMP_Text remainingTimeText;

    [SerializeField]
    private TMP_Text dateText;

    private void OnEnable()
    {
        GameEvents.OnTravelStarted += Open;
        GameEvents.OnTravelFinished += CloseAndReturnToCity;
        GameEvents.OnTimeAdvanced += Refresh;
        GameEvents.OnTravelEventResolved += Refresh;
    }

    private void OnDisable()
    {
        GameEvents.OnTravelStarted -= Open;
        GameEvents.OnTravelFinished -= CloseAndReturnToCity;
        GameEvents.OnTimeAdvanced -= Refresh;
        GameEvents.OnTravelEventResolved -= Refresh;
    }

    public void Open()
    {
        cityPanel.SetActive(false);
        travelPanel.SetActive(false);
        travelProgressPanel.SetActive(true);

        Refresh();
    }

    public void Refresh()
    {
        if (!TravelManager.Instance.IsTraveling)
            return;

        var travel =
            TravelManager
                .Instance
                .CurrentTravel;

        CityData origin =
            DatabaseManager
                .Instance
                .GetData<CityData>(
                    travel.OriginCityID
                );

        CityData destination =
            DatabaseManager
                .Instance
                .GetData<CityData>(
                    travel.DestinationCityID
                );

        if (origin == null ||
            destination == null)
        {
            routeText.text =
                "Rota de viagem nao encontrada.";

            remainingTimeText.text =
                $"Tempo restante: {travel.RemainingHours}h";

            dateText.text =
                TimeManager
                    .Instance
                    .GetFormattedDate();

            return;
        }

        routeText.text =
            $"{origin.DisplayName} -> {destination.DisplayName}";

        remainingTimeText.text =
            $"Tempo restante: {travel.RemainingHours}h " +
            $"de {travel.TotalHours}h";

        dateText.text =
            TimeManager
                .Instance
                .GetFormattedDate();
    }

    public void ContinueTravel()
    {
        TravelManager
            .Instance
            .ContinueTravel();

        Refresh();
    }

    public void Camp()
    {
        Debug.Log(
            "Camp system will be added later."
        );
    }

    private void CloseAndReturnToCity()
    {
        travelProgressPanel.SetActive(false);
        travelPanel.SetActive(false);
        cityPanel.SetActive(true);
    }
}
