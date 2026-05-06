using TMPro;
using UnityEngine;

public class BottomTutorialController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public TMP_Text buttonHintText;
    public GameObject dialogueRoot;

    [Header("Tutorial Arrows")]
    public GameObject arrowSetup;
    public GameObject arrowSprayer;
    public GameObject arrowSelectDrop;
    public GameObject arrowLight;
    public GameObject arrowCapacitor;
    public GameObject arrowVoltageKnob;

    [Header("Spray Teaching Radius")]
    public SpraySpawner spraySpawner;

    [Header("Disable These During Tutorial")]
    public Behaviour[] componentsToDisableDuringTutorial;

    public static bool TutorialInputLocked { get; private set; }

    public bool IsSessionActive => tutorialSessionActive;

    private bool tutorialSessionActive = false;
    private int currentStep = 0;

    private const int LastStepIndex = 39;

    private void Start()
    {
        if (dialogueRoot == null)
            dialogueRoot = gameObject;

        ResetTutorialProgress();

        tutorialSessionActive = false;
        TutorialInputLocked = false;

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    private void Update()
    {
        if (!tutorialSessionActive)
            return;

        if (OVRInput.GetDown(OVRInput.Button.One))
            TryAdvanceFromPlayerReply();
    }

    public void BeginTutorialSession()
    {
        ResetTutorialProgress();

        tutorialSessionActive = true;
        TutorialInputLocked = true;

        SetTutorialInputComponentsEnabled(false);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        ShowStep();
    }

    public void EndTutorialSession()
    {
        tutorialSessionActive = false;
        TutorialInputLocked = false;

        if (spraySpawner != null)
            spraySpawner.ReturnToRandomModeAndClearDrops();

        SetTutorialInputComponentsEnabled(true);
        HideAllArrows();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        ResetTutorialProgress();
    }

    private void ResetTutorialProgress()
    {
        currentStep = 0;
    }

    private void TryAdvanceFromPlayerReply()
    {
        if (IsTaskStep(currentStep))
        {
            RefreshTaskHint();
            return;
        }

        NextStep();
    }

    public void NextStep()
    {
        if (currentStep < LastStepIndex)
        {
            currentStep++;
            ShowStep();
        }
        else
        {
            EndTutorialSession();
        }
    }

    private void ShowStep()
    {
        ApplyStepSideEffects(currentStep);

        if (dialogueText != null)
            dialogueText.text = GetDialogueForCurrentStep();

        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();

        UpdateArrowForStep(currentStep);
    }

    private void ApplyStepSideEffects(int step)
    {
        if (spraySpawner == null)
        {
            if (step == 15 || step == 17 || step == 19 || step == 23)
                Debug.LogWarning("[BottomTutorialController] SpraySpawner is not assigned.");

            return;
        }

        switch (step)
        {
            case 15:
                spraySpawner.SetTeachingRadiusStep(0);
                break;

            case 17:
                spraySpawner.SetTeachingRadiusStep(1);
                break;

            case 19:
                spraySpawner.SetTeachingRadiusStep(2);
                break;

            case 21:
                spraySpawner.ReturnToRandomModeAndClearDrops();
                break;
        }
    }

    private bool IsTaskStep(int step)
    {
        return step == 12 || step == 15 || step == 17 || step == 19 || step == 23 || step == 25 || step == 29;
    }

    private void RefreshTaskHint()
    {
        if (buttonHintText != null)
            buttonHintText.text = GetButtonHintForCurrentStep();
    }

    private string GetDialogueForCurrentStep()
    {
        switch (currentStep)
        {
            case 0:
                return "Ah. Ein Klassenzimmer. Gut. Das kenne ich. Bei uns sahen sie etwas anders aus — aber das Prinzip ist dasselbe. Mein Name ist Robert Andrews Millikan. Ich war Physikprofessor an der University of Chicago — und später am California Institute of Technology.";

            case 1:
                return "Aber egal… Ich habe ein Problem. Oder genauer gesagt: Ich habe eine Frage — und ich brauche jemanden, der mir hilft, sie zu beantworten. Ist elektrische Ladung unteilbar? Gibt es ein kleinstes elektrisches Paket — eine Art Atom der Ladung — oder fließt Elektrizität einfach kontinuierlich, wie Wasser durch einen Schlauch?";

            case 2:
                return "Ich habe ein Experiment gebaut, das diese Frage beantworten kann. Aber ich kann es nicht alleine durchführen. Dazu brauche ich einen Assistenten — wie dich! Du müsstest die Geräte bedienen, während ich erkläre, was gerade passiert. Außerdem musst du gut aufpassen. Genau wie mein Doktorand Harvey Fletcher damals.";

            case 3:
                return "Ausgezeichnet. Dann legen wir los. Komm zum Experiment — ich zeige dir, womit wir es zu tun haben.";

            case 4:
                return "Gut. Dann fangen wir von vorne an. Ich bin 1868 in Morrison, Illinois geboren. Physik hat mich schon immer fasziniert. Die Frage, woraus Materie wirklich besteht. Was Elektrizität eigentlich ist. Was hinter den Gleichungen steckt.";

            case 5:
                return "1909 haben mein Doktorand Harvey Fletcher und ich begonnen, diesen Apparat hier zu entwickeln. Fletcher hatte die entscheidende Idee: Statt Wasser — Öl. Unsere Tröpfchen bleiben stundenlang stabil. Das klingt banal. Aber es hat alles verändert.";

            case 6:
                return "Was wir herausfinden wollten: Gibt es eine kleinste Einheit elektrischer Ladung — oder ist Elektrizität so etwas wie eine Flüssigkeit, die man beliebig klein aufteilen kann? J.J. Thomson hatte 1897 gezeigt, dass es Elektronen gibt — kleine, negativ geladene Teilchen. Aber wie groß ist ihre Ladung? Das wusste niemand genau. Ich wollte es wissen. Und heute erfährst du, wie ich es gemessen habe.";

            case 7:
                return "Hier ist er. Mein Apparat. Fünf Dinge arbeiten zusammen — und jedes einzelne ist entscheidend. Ich erkläre dir jede einzelne Komponente nacheinander.";

            case 8:
                return "Dieser einfache Zerstäuber — fast wie ein Parfümflakon — ist der Anfang von allem. Ein kurzer Druck, und Millionen winziger Öltröpfchen werden in die Messkammer geblasen. Durch die Reibung beim Zerstäuben laden sich viele davon elektrisch auf. Genau das sind die entscheidenden Öltröpfchen für uns.";

            case 9:
                return "Diese Tröpfchen sind viel zu klein, um sie direkt zu sehen. Das Mikroskop macht sie sichtbar — als helle Lichtpunkte auf dunklem Hintergrund. Aber Vorsicht: Das Mikroskop spiegelt das Bild. Was wir sehen, sinkt in Wirklichkeit — es sieht aus, als würde es steigen. Das verwirrt am Anfang. Deshalb haben wir hier in der Simulation die Öltröpfchen für das bloße Auge sichtbar gemacht und wir sehen direkt, ob die Öltröpfchen sinken oder steigen.";

            case 10:
                return "Das Licht kommt schräg von der Seite. Ohne es würden wir gar nichts sehen. Die Tröpfchen streuen das Licht wie Staubkörner in einem Sonnenstrahl — plötzlich leuchten sie auf.";

            case 11:
                return "Das Herzstück. Zwei Metallplatten, exakt 6 Millimeter auseinander. Wenn ich eine Spannung anlege, entsteht zwischen ihnen ein elektrisches Feld — gleichmäßig, kontrolliert. Dieses Feld wird auf unsere Tröpfchen wirken. Wie stark, das liegt in unserer Hand. Dieser Regler ist unser wichtigstes Werkzeug. Er bestimmt, wie stark das elektrische Feld zwischen den Platten ist.";

            case 12:
                return "Das Feld ist ausgeschaltet. Bitte geh mit deiner Hand zum Zerstäuber und drücke den Trigger.";

            case 13:
                return "Wie du siehst, werden die ersten Tröpfchen in den Apparat gesprüht. Die Tröpfchen fallen. Langsam — aber sie fallen. Die Schwerkraft zieht sie nach unten. Das ist die erste Kraft, mit der wir es zu tun haben.";

            case 14:
                return "Warum ist das wichtig? Weil sich aus der Fallgeschwindigkeit eines Tröpfchens der Radius r berechnen lässt. Wir benötigen den Radius, um im nächsten Schritt die Ladung bestimmen zu können. Die Dichte des Öls kenne ich — 875 Kilogramm pro Kubikmeter. Die Erdbeschleunigung kennst du. Was ich nicht kenne: den Radius des Tröpfchens. Den messe ich aus der Fallgeschwindigkeit. Schnelleres Fallen bedeutet: größeres Tröpfchen. Einfacher Zusammenhang — aber fundamental wichtig.";

            case 15:
                return "Stelle nacheinander folgende Tröpfchengrößen ein und beobachte die Fallgeschwindigkeit: r = 0,5 µm — sehr langsam fallend.";

            case 16:
                return "Gut. Kleine Tröpfchen fallen sehr langsam. Der Radius bestimmt, wie schnell ein Tröpfchen fällt.";

            case 17:
                return "Stelle nun die nächste Tröpfchengröße ein: r = 1,0 µm — mittlere Geschwindigkeit.";

            case 18:
                return "Du siehst: Wenn der Radius größer wird, fällt das Tröpfchen schneller.";

            case 19:
                return "Stelle nun die dritte Tröpfchengröße ein: r = 1,5 µm — schnell fallend.";

            case 20:
                return "Gut. Du verstehst jetzt: Der Radius bestimmt, wie schnell ein Tröpfchen fällt. Und aus der Fallgeschwindigkeit können wir den Radius berechnen. Jetzt kommt der eigentliche Schritt.";

            case 21:
                return "Die Radius-Vergleiche sind abgeschlossen. Für die eigentliche Messung verwenden wir wieder normale, zufällig erzeugte Öltröpfchen.";

            case 22:
                return "Wir sehen einen Regler, um die Spannung im Feld einzustellen. Dreh den Regler mal hoch und achte auf das ausgewählte Tröpfchen. Das Tröpfchen verändert seine Geschwindigkeit. Die elektrische Kraft — die Coulomb-Kraft — wirkt. Je mehr Spannung, desto stärker.";

            case 23:
                return "Erzeuge nun eine neue zufällige Gruppe von Öltröpfchen.";

            case 24:
                return "Dreh den Spannungsregler vor dir langsam nach oben. Schau mal, der grüne Pfeil wächst. Die elektrische Kraft wird stärker. Das Tröpfchen verlangsamt sich. Wenn die Spannung zu hoch wird, dann steigt das Tröpfchen auf einmal.";

            case 25:
                return "Wähle nun ein Tröpfchen mit dem roten Strahl aus.";

            case 26:
                return "Versuch es mal so einzustellen, dass du es zum Schweben bringst. So, dass das Tröpfchen hängt, als würde die Zeit stillstehen. Das elektrische Feld hält es exakt gegen die Schwerkraft. Die beiden Kräfte heben sich exakt auf.";

            case 27:
                return "Und aus diesem Gleichgewicht folgt alles. Wenn das Tröpfchen schwebt, weiß ich: Die elektrische Kraft ist gleich der Schwerkraft. Ich kenne die Masse — aus dem Radius, den wir gerade gemessen haben. Ich kenne den Plattenabstand: 6 Millimeter. Und die Spannung lese ich ab. Damit berechne ich die Ladung q.";

            case 28:
                return "q ist die Ladung des Tröpfchens in Coulomb. U ist die Spannung am Kondensator in Volt. d ist der Plattenabstand in Meter. m ist die Masse des Tröpfchens in Kilogramm. g ist die Erdbeschleunigung. E ist die elektrische Feldstärke.";

            case 29:
                return "Stelle nun die Spannung so ein, dass das ausgewählte Tröpfchen möglichst schwebt.";

            case 30:
                return "Das war deine erste Ladungsmessung. Aber eine Messung ist noch keine Wissenschaft — das ist nur ein Datenpunkt. Was ich brauche, ist ein Muster.";

            case 31:
                return "Ich habe nicht ein Tröpfchen gemessen. Ich habe hunderte gemessen. Über Monate. Und dabei etwas Erstaunliches beobachtet: Die Ladungen, die ich gemessen habe, waren nie zufällig verteilt. Sie häuften sich immer an denselben Stellen. Immer ein Vielfaches derselben Grundeinheit. Einfach. Doppelt. Dreifach. Viermal. Nie dazwischen. Die Natur schien zu zählen — in ganzen Zahlen.";

            case 32:
                return "Elektrische Ladung ist nicht kontinuierlich. Sie kommt in Paketen. Das kleinste Paket — das ist die Elementarladung e. Jedes Tröpfchen trägt genau ein, zwei, drei oder mehr dieser Pakete. Nie einen Bruchteil. Das nenne ich Ladungsquantisierung.";

            case 33:
                return "Siehst du es? Die Ladungen häufen sich. Nicht zufällig — bei bestimmten Werten. Bei ganzzahligen Vielfachen. Das Muster wird sichtbar. Das ist Wissenschaft. Nicht eine Messung — sondern ein Muster aus vielen Messungen. Und das Muster ist eindeutig: Elektrische Ladung ist gequantelt. Es gibt eine kleinste Einheit.";

            case 34:
                return "Ich muss dir etwas zeigen. Etwas, das 1978 ein Historiker namens Gerald Holton entdeckt hat — in meinen Original-Notizbüchern aus den Jahren 1911 und 1912. Er fand heraus, dass ich weit mehr Tröpfchen gemessen hatte als ich je veröffentlicht habe.";

            case 35:
                return "Neben manchen Datenpunkten standen meine handschriftlichen Anmerkungen: 'Won't work'. 'Schiefe Messung'. 'Error — discard'. War das falsch? Ich glaube: Nein. Ich habe Messungen ausgeschlossen, bei denen ich technische Fehler erkannt habe — Luftzug, Erschütterungen, einen zitternden Tropfen. Das ist kein Betrug. Das ist Urteilsvermögen.";

            case 36:
                return "Aber der Historiker Allan Franklin hat 1981 gezeigt: Die Daten, die ich wegließ, hätten meinen Endwert kaum verändert. Nur die statistische Unsicherheit wäre größer geworden — von 0,2 Prozent auf fast 2 Prozent. Die Selektion hat meine Präzision verbessert, aber nicht mein Ergebnis.";

            case 37:
                return "Die Frage, wann Datenselektion legitim ist, beschäftigt Wissenschaftler bis heute. Es gibt keine einfache Antwort. Aber es gibt eine klare Anforderung: Transparenz. Was ich ausschließe — und warum — das muss dokumentiert sein.";

            case 38:
                return "Es war 1913. Ich publizierte meinen Endwert: e = 1,592 mal zehn hoch minus neunzehn Coulomb. Unsicherheit: 0,2 Prozent. Es ist die genaueste Messung der Elementarladung, die es bis dahin gibt. Der heute akzeptierte Wert ist 1,602 — die Abweichung kommt aus einem leicht falschen Literaturwert für die Luftviskosität, den ich damals verwendet habe. Nicht aus meiner Methode.";

            case 39:
                return "Aber der eigentliche Beitrag ist nicht die Zahl. Es ist das Prinzip. Elektrische Ladung ist gequantelt. Es gibt keine halbe Elementarladung. Keine viertel Elementarladung. Die Natur zählt in ganzen Zahlen. Das ist fundamental. Das ist eine der tiefsten Strukturen der Materie. Ich danke dir dafür. Du hast heute sehr viel gelernt, lass uns jetzt mal schauen, wie viel du davon behalten hast.";

            default:
                return "";
        }
    }

    private string GetButtonHintForCurrentStep()
    {
        switch (currentStep)
        {
            case 12:
                return "Aufgabe: Zerstäuber benutzen.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 15:
                return "Aufgabe: r = 0,5 µm.\nErzeuge eine Gruppe kleiner Tröpfchen.\nDrücke den Trigger.";

            case 17:
                return "Aufgabe: r = 1,0 µm.\nErzeuge die nächste Tröpfchengruppe.\nDrücke den Trigger.";

            case 19:
                return "Aufgabe: r = 1,5 µm.\nErzeuge die dritte Tröpfchengruppe.\nDrücke den Trigger.";

            case 23:
                return "Aufgabe: Zufällige Öltröpfchen erzeugen.\nRichte auf den Zerstäuber.\nDrücke den Trigger.";

            case 25:
                return "Aufgabe: Tröpfchen auswählen.\nZiele mit dem roten Strahl auf ein Tröpfchen.\nDrücke den Trigger.";

            case 29:
                return "Aufgabe: Spannung einstellen.\nGreife den Spannungsregler.\nStelle das Tröpfchen möglichst ruhig ein.";

            default:
                return "A: Weiter";
        }
    }

    private void UpdateArrowForStep(int step)
    {
        HideAllArrows();

        switch (step)
        {
            case 7:
                if (arrowSetup != null) arrowSetup.SetActive(true);
                break;

            case 8:
            case 12:
            case 15:
            case 17:
            case 19:
            case 23:
                if (arrowSprayer != null) arrowSprayer.SetActive(true);
                break;

            case 10:
                if (arrowLight != null) arrowLight.SetActive(true);
                break;

            case 11:
            case 22:
            case 27:
            case 28:
                if (arrowCapacitor != null) arrowCapacitor.SetActive(true);
                break;

            case 24:
            case 29:
                if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(true);
                break;

            case 25:
                if (arrowSelectDrop != null) arrowSelectDrop.SetActive(true);
                break;
        }
    }

    private void HideAllArrows()
    {
        if (arrowSetup != null) arrowSetup.SetActive(false);
        if (arrowSprayer != null) arrowSprayer.SetActive(false);
        if (arrowSelectDrop != null) arrowSelectDrop.SetActive(false);
        if (arrowLight != null) arrowLight.SetActive(false);
        if (arrowCapacitor != null) arrowCapacitor.SetActive(false);
        if (arrowVoltageKnob != null) arrowVoltageKnob.SetActive(false);
    }

    public void NotifyDropletTriggered()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 12 || currentStep == 15 || currentStep == 17 || currentStep == 19 || currentStep == 23)
            NextStep();
    }

    public void NotifyDropSelected()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 25)
            NextStep();
    }

    public void NotifyVoltageSolved()
    {
        if (!tutorialSessionActive)
            return;

        if (currentStep == 29)
            NextStep();
    }

    private void SetTutorialInputComponentsEnabled(bool enabled)
    {
        if (componentsToDisableDuringTutorial == null)
            return;

        for (int i = 0; i < componentsToDisableDuringTutorial.Length; i++)
        {
            if (componentsToDisableDuringTutorial[i] != null)
                componentsToDisableDuringTutorial[i].enabled = enabled;
        }
    }
}