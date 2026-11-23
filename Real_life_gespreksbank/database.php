<?php
// auteur: Elif
// functie: beschrijving database

require_once 'config.php';

class Database {
    private PDO $conn;

    public function __construct() {
        $this->conn = $this->connectDb();
    }

    public function connectDb(): PDO {
        try {
            $conn = new PDO("mysql:host=" . DB_SERVERNAME . ";dbname=" . DB_NAME . ";charset=utf8mb4", DB_USERNAME, DB_PASSWORD, [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC
            ]);
            //echo "Connected successfully";
            return $conn;
        }
        catch(PDOException $e) {
            echo "Connection failed: " . $e->getMessage();
            exit;
        }
    }

    // Alle categorienamen ophalen
    public function getCategories(): array {
        $stmt = $this->conn->query("SELECT name FROM categories ORDER BY id");
        return $stmt->fetchAll(PDO::FETCH_COLUMN);
    }

    // Alle vragen ophalen, met de naam van hun categorie
    public function getQuestions(): array {
        $stmt = $this->conn->query(
            "SELECT questions.text, categories.name AS category
             FROM questions
             JOIN categories ON categories.id = questions.category_id"
        );
        return $stmt->fetchAll();
    }

    // Het id van een categorie zoeken op naam
    public function getCategoryId(string $name) {
        $stmt = $this->conn->prepare("SELECT id FROM categories WHERE name = ?");
        $stmt->execute([$name]);
        return $stmt->fetchColumn();
    }

    // Kijken of een vraag al bestaat
    public function questionExists(string $text): bool {
        $stmt = $this->conn->prepare("SELECT id FROM questions WHERE text = ?");
        $stmt->execute([$text]);
        return (bool) $stmt->fetch();
    }

    // Een vraag opslaan
    public function addQuestion(int $categoryId, string $text): void {
        $stmt = $this->conn->prepare("INSERT INTO questions (category_id, text) VALUES (?, ?)");
        $stmt->execute([$categoryId, $text]);
    }
}
?>