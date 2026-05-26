using UnityEngine;
using TMPro;

public class TravelUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject travelPanel;

    [SerializeField]
    private GameObject cityPanel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text currentCityText;

    [Header("Containers")]
    [SerializeField]
    private Transform destinationContainer;

    [Header("Prefabs")]
    [SerializeField]
    private UIButtonEntry buttonPrefab;

    public void Open()
    {
        cityPanel.SetActive(false);
        travelPanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        travelPanel.SetActive(false);
        cityPanel.SetActive(true);
    }

    private void Refresh()
    {
        ClearContainer(destinationContainer);

        CityData currentCity =
            CityManager
                .Instance
                .CurrentCity;

        currentCityText.text =
            $"Partindo de: {currentCity.DisplayName}";

        foreach (var connection
            in currentCity.Connections)
        {
            if (connection == null ||
                connection.ConnectedCity == null)
            {
                continue;
            }

            CityData destination =
                connection.ConnectedCity;

            int travelHours =
                connection.TravelHours;

            UIButtonEntry entry =
                Instantiate(
                    buttonPrefab,
                    destinationContainer
                );

            string label =
                $"{destination.DisplayName}\n" +
                $"{travelHours}h • Segurança {destination.Security}/10";

            entry.Setup(
                label,
                () => ConfirmTravel(destination)
            );
        }
    }

    private void ConfirmTravel(
        CityData destination)
    {
        Debug.Log(
            $"Selected destination: {destination.DisplayName}"
        );

        travelPanel.SetActive(false);

        TravelManager
            .Instance
            .TravelTo(destination);
    }

    private void ClearContainer(
        Transform container)
    {
        for (int i =
            container.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                container
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTravelFinished += HandleTravelFinished;
    }

    private void OnDisable()
    {
        GameEvents.OnTravelFinished -= HandleTravelFinished;
    }

    private void HandleTravelFinished()
    {
        travelPanel.SetActive(false);
        cityPanel.SetActive(true);
    }
}