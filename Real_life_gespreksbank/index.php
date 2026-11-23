<?php
// functie: gespreksbank pagina

require_once 'database.php';

$db = new Database();
$melding = "";

// Is er een eigen vraag verstuurd?
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $text       = trim($_POST['text'] ?? '');
    $category   = trim($_POST['category'] ?? '');
    $categoryId = $db->getCategoryId($category);

    if (mb_strlen($text) < 10) {
        $melding = "Vul een volledige vraag in van minimaal 10 tekens.";
    } elseif (!$categoryId) {
        $melding = "Onbekende categorie.";
    } elseif ($db->questionExists($text)) {
        $melding = "Deze vraag bestaat al.";
    } else {
        $db->addQuestion((int) $categoryId, $text);
        $melding = "Je vraag is toegevoegd!";
    }
}

// Gegevens ophalen uit de database
$categories = $db->getCategories();
$questions  = $db->getQuestions();

// Vragen omzetten naar [tekst, categorie] voor de JavaScript
$questionList = [];
foreach ($questions as $q) {
    $questionList[] = [$q['text'], $q['category']];
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>GESPREKSBANK</title>
<link rel="stylesheet" href="style.css">
</head>

<body>

<header>
    <div class="logo">GESPREKSBANK</div>
</header>

<main>
    <div class="question-container">
        <div class="category" id="category"></div>
        <div class="question" id="question"></div>
    </div>
</main>

<footer>
    <button class="nav-button left" onclick="previousQuestion()">
        ← Terug
    </button>

    <button class="nav-button center" onclick="openModal()">
        + Eigen vraag
    </button>

    <button class="nav-button right" onclick="nextQuestion()">
        Volgende →
    </button>
</footer>

<!-- ADD QUESTION MODAL -->

<div class="modal" id="modal">
    <div class="modal-content">
        <h2>Voeg een vraag toe</h2>

        <form method="post" action="index.php">
            <input
                type="text"
                name="text"
                placeholder="Typ jouw vraag..."
                minlength="10"
                maxlength="200"
                required
            >

            <select name="category">
                <?php foreach ($categories as $name): ?>
                    <option><?= htmlspecialchars($name) ?></option>
                <?php endforeach; ?>
            </select>

            <button type="submit" class="add-btn">
                Vraag toevoegen
            </button>

            <button type="button" class="close-btn" onclick="closeModal()">
                Annuleren
            </button>
        </form>
    </div>
</div>

<script>

// VRAGEN UIT DE DATABASE (door PHP in de pagina gezet)

let allQuestions = <?= json_encode($questionList, JSON_UNESCAPED_UNICODE | JSON_HEX_TAG | JSON_HEX_AMP) ?>;
let currentIndex = 0;

// MELDING NA HET TOEVOEGEN

<?php if ($melding !== ""): ?>
alert(<?= json_encode($melding, JSON_UNESCAPED_UNICODE) ?>);
<?php endif; ?>

// SHUFFLE (Fisher-Yates)

function shuffleQuestions() {
    for (let i = allQuestions.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [allQuestions[i], allQuestions[j]] = [allQuestions[j], allQuestions[i]];
    }
}

// DISPLAY QUESTION

function showQuestion() {
    const q = allQuestions[currentIndex];

    document.getElementById("question").textContent = q[0];
    document.getElementById("category").textContent = q[1];

    const questionElement = document.getElementById("question");
    questionElement.style.animation = "none";
    void questionElement.offsetWidth;
    questionElement.style.animation = "appear 0.3s ease";
}

// NEXT QUESTION
// Alle vragen komen één keer voorbij, daarna volgt automatisch een nieuwe shuffle.

function nextQuestion() {
    if (allQuestions.length === 0) return;

    if (currentIndex < allQuestions.length - 1) {
        currentIndex++;
    } else {
        const lastQuestion = allQuestions[currentIndex];

        shuffleQuestions();

        // Voorkom dat dezelfde vraag twee keer achter elkaar komt
        if (allQuestions[0] === lastQuestion && allQuestions.length > 1) {
            const swapWith = 1 + Math.floor(Math.random() * (allQuestions.length - 1));
            [allQuestions[0], allQuestions[swapWith]] =
                [allQuestions[swapWith], allQuestions[0]];
        }

        currentIndex = 0;
    }

    showQuestion();
}

// PREVIOUS QUESTION

function previousQuestion() {
    if (allQuestions.length === 0) return;

    currentIndex--;

    if (currentIndex < 0) {
        currentIndex = allQuestions.length - 1;
    }

    showQuestion();
}

// OPEN / CLOSE MODAL

function openModal() {
    document.getElementById("modal").style.display = "flex";
}

function closeModal() {
    document.getElementById("modal").style.display = "none";
}

// KEYBOARD NAVIGATION

document.addEventListener("keydown", function(event) {
    if (event.key === "ArrowRight") {
        nextQuestion();
    }

    if (event.key === "ArrowLeft") {
        previousQuestion();
    }

    if (event.key === "Escape") {
        closeModal();
    }
});

// START

if (allQuestions.length === 0) {
    document.getElementById("question").textContent =
        "Er staan nog geen vragen in de database.";
} else {
    shuffleQuestions();
    showQuestion();
}

</script>

</body>
</html>