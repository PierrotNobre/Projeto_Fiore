using UnityEngine;
using TMPro;

public class TravelEventUIController
    : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject travelProgressPanel;

    [SerializeField]
    private GameObject travelEventPanel;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text eventTitleText;

    [SerializeField]
    private TMP_Text eventDescriptionText;

    [Header("Choices")]
    [SerializeField]
    private Transform choiceContainer;

    [SerializeField]
    private UIButtonEntry buttonPrefab;

    private void OnEnable()
    {
        GameEvents.OnTravelEventTriggered += Open;
        GameEvents.OnTravelEventResolved += Close;
    }

    private void OnDisable()
    {
        GameEvents.OnTravelEventTriggered -= Open;
        GameEvents.OnTravelEventResolved -= Close;
    }

    private void Open(
        TravelEventData eventData)
    {
        travelProgressPanel
            .SetActive(false);

        travelEventPanel
            .SetActive(true);

        eventTitleText.text =
            eventData.DisplayName;

        eventDescriptionText.text =
            eventData.EventText;

        BuildChoices(eventData);
    }

    private void BuildChoices(
        TravelEventData eventData)
    {
        ClearContainer(
            choiceContainer
        );

        for (int i = 0;
            i < eventData.Choices.Count;
            i++)
        {
            int choiceIndex = i;

            EventChoice choice =
                eventData.Choices[i];

            UIButtonEntry entry =
                Instantiate(
                    buttonPrefab,
                    choiceContainer
                );

            entry.Setup(
                choice.ChoiceText,
                () => SelectChoice(choiceIndex)
            );
        }
    }

    private void SelectChoice(
        int choiceIndex)
    {
        TravelEventManager
            .Instance
            .ResolveChoice(
                choiceIndex
            );
    }

    private void Close()
    {
        travelEventPanel
            .SetActive(false);

        travelProgressPanel
            .SetActive(true);
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
}