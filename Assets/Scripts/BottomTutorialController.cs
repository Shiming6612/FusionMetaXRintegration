using TMPro;
using UnityEngine;

public class BottomTutorialController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text buttonHintText;
    public GameObject dialogueRoot;

    [Header("Arrow Targets")]
    public GameObject tutorialArrow;
    public Transform targetSetup;
    public Transform targetSprayer;
    public Transform targetDropletArea;
    public Transform targetVoltageKnob;

    [Header("Tutorial Progress")]
    public bool dropletTriggered = false;
    public bool dropSelected = false;
    public bool voltageSolved = false;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    public static bool TutorialInputLocked { get; private set; }

    private int currentStep = 0;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        TutorialInputLocked = true;
        SetTutorialInputComponentsEnabled(false);
        ShowStep();
    }

    private void OnEnable()
    {
        TutorialInputLocked = true;
        SetTutorialInputComponentsEnabled(false);
    }

    private void OnDisable()
    {
        TutorialInputLocked = false;
        SetTutorialInputComponentsEnabled(true);
    }

    private void Update()
    {
        // A = Weiter
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            TryNextStep();
        }

        // B = Zurück
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            PreviousStep();
        }
    }

    private void TryNextStep()
    {
        // Schritt 3: Öltröpfchen auslösen
        if (currentStep == 3 && !dropletTriggered)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Bitte löse zuerst ein Tröpfchen aus.    B = Zurück";
            return;
        }

        // Schritt 6: Tröpfchen auswählen
        if (currentStep == 6 && !dropSelected)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Bitte wähle zuerst ein Tröpfchen aus.    B = Zurück";
            return;
        }

        // Schritt 9: Spannung korrekt einstellen
        if (currentStep == 9 && !voltageSolved)
        {
            if (buttonHintText != null)
                buttonHintText.text = "Bitte stelle zuerst die richtige Spannung ein.    B = Zurück";
            return;
        }

        NextStep();
    }

    public void NextStep()
    {
        if (currentStep < 10)
        {
            currentStep++;
            ShowStep();
        }
        else
        {
            EndTutorial();
        }
    }

    public void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            ShowStep();
        }
    }

    private void ShowStep()
    {
        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = "A = Weiter    B = Zurück";

        UpdateArrowForStep(currentStep);
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Willkommen im VR-Labor. Gehe bitte zum Experimentaufbau.";
            case 1:
                return "Dies ist der Millikan-Versuch. Hier untersuchen wir geladene Öltröpfchen in einem elektrischen Feld.";
            case 2:
                return "Hier siehst du wichtige Bestandteile des Aufbaus. Zuerst schauen wir uns den Zerstäuber an.";
            case 3:
                return "Bitte löse jetzt ein Öltröpfchen aus.";
            case 4:
                return "Gut gemacht! Du hast erfolgreich ein Tröpfchen erzeugt.";
            case 5:
                return "Danach lernst du, wie du mit dem roten Strahl ein Tröpfchen auswählst.";
            case 6:
                return "Bitte wähle jetzt ein Tröpfchen mit dem roten Strahl aus.";
            case 7:
                return "Sehr gut! Das Tröpfchen wurde ausgewählt und die Parameter werden im Panel angezeigt.";
            case 8:
                return "Jetzt kommt die Spannungsregelung. Verwende die angezeigte Masse und Ladung sowie die Formel U = mgd / q. Der Plattenabstand beträgt d = 6 mm.";
            case 9:
                return "Berechne nun die richtige Spannung selbst und stelle sie am Regler ein. Der Spannungsbereich liegt zwischen 0 und 800 V.";
            case 10:
                return "Glückwunsch! Du hast das Tutorial abgeschlossen. Viel Spaß beim Experimentieren.";
            default:
                return "";
        }
    }

    private void UpdateArrowForStep(int step)
    {
        if (tutorialArrow == null)
            return;

        tutorialArrow.SetActive(false);

        switch (step)
        {
            case 0:
            case 1:
                if (targetSetup != null)
                {
                    tutorialArrow.SetActive(true);
                    tutorialArrow.transform.position = targetSetup.position;
                }
                break;

            case 2:
            case 3:
            case 4:
                if (targetSprayer != null)
                {
                    tutorialArrow.SetActive(true);
                    tutorialArrow.transform.position = targetSprayer.position;
                }
                break;

            case 5:
            case 6:
            case 7:
                if (targetDropletArea != null)
                {
                    tutorialArrow.SetActive(true);
                    tutorialArrow.transform.position = targetDropletArea.position;
                }
                break;

            case 8:
            case 9:
                if (targetVoltageKnob != null)
                {
                    tutorialArrow.SetActive(true);
                    tutorialArrow.transform.position = targetVoltageKnob.position;
                }
                break;

            case 10:
                tutorialArrow.SetActive(false);
                break;
        }
    }

    public void NotifyDropletTriggered()
    {
        dropletTriggered = true;

        if (currentStep == 3)
        {
            currentStep = 4;
            ShowStep();
        }
    }

    public void NotifyDropSelected()
    {
        dropSelected = true;

        if (currentStep == 6)
        {
            currentStep = 7;
            ShowStep();
        }
    }

    public void NotifyVoltageSolved()
    {
        if (voltageSolved)
            return;

        voltageSolved = true;

        if (currentStep == 9)
        {
            currentStep = 10;
            ShowStep();
        }
    }

    private void EndTutorial()
    {
        TutorialInputLocked = false;
        SetTutorialInputComponentsEnabled(true);

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (tutorialArrow != null)
            tutorialArrow.SetActive(false);
    }

    private void SetTutorialInputComponentsEnabled(bool enabled)
    {
        if (componentsToDisableDuringTutorial == null)
            return;

        for (int i = 0; i < componentsToDisableDuringTutorial.Length; i++)
        {
            if (componentsToDisableDuringTutorial[i] != null)
            {
                componentsToDisableDuringTutorial[i].enabled = enabled;
            }
        }
    }
}