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

    [SerializeField]
    private TMP_Text dateText;

    [SerializeField]
    private TMP_Text feedbackText;

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

        SetFeedback(
            "Escolha um destino disponivel."
        );

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

        if (currentCity == null)
        {
            currentCityText.text =
                "Cidade atual nao encontrada.";

            return;
        }

        currentCityText.text =
            $"Partindo de: {currentCity.DisplayName}";

        if (dateText != null)
        {
            dateText.text =
                TimeManager
                    .Instance
                    .GetFormattedDate();
        }

        if (currentCity.Connections == null ||
            currentCity.Connections.Count == 0)
        {
            SetFeedback(
                "Nenhum destino conectado a esta cidade."
            );

            return;
        }

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
                Mathf.Max(
                    1,
                    connection.TravelHours
                );

            UIButtonEntry entry =
                Instantiate(
                    buttonPrefab,
                    destinationContainer
                );

            string label =
                BuildDestinationLabel(
                    destination,
                    connection,
                    travelHours
                );

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

        SetFeedback(
            $"Iniciando viagem para {destination.DisplayName}..."
        );

        TravelManager
            .Instance
            .TravelTo(destination);

        if (TravelManager.Instance.IsTraveling)
        {
            travelPanel.SetActive(false);
        }
    }

    private string BuildDestinationLabel(
        CityData destination,
        CityConnection connection,
        int travelHours)
    {
        string description =
            !string.IsNullOrEmpty(
                connection.RouteDescription)
                ? connection.RouteDescription
                : destination.Description;

        if (string.IsNullOrEmpty(description))
        {
            description =
                "Sem descricao.";
        }

        string label =
            $"{destination.DisplayName}\n" +
            $"{description}\n" +
            $"Duracao: {travelHours}h";

        if (connection.TravelCost > 0)
        {
            label +=
                $" - Custo: {connection.TravelCost} ouro";
        }

        if (connection.RiskLevel > 0)
        {
            label +=
                $" - Risco: {connection.RiskLevel}/10";
        }

        label +=
            $"\nSeguranca local: {destination.Security}/10";

        return label;
    }

    private void SetFeedback(
        string message)
    {
        if (feedbackText == null)
            return;

        feedbackText.text =
            message;
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

        CityData city =
            CityManager
                .Instance
                .CurrentCity;

        if (city != null)
        {
            SetFeedback(
                $"Chegada em {city.DisplayName}."
            );
        }
    }
}
