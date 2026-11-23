<?php
// auteur: Elif
// functie: config

// Docker geeft deze waarden door via omgevingsvariabelen.
// Zonder omgevingsvariabelen blijven de lokale standaardwaarden gelden.
define('DB_SERVERNAME', getenv('DB_SERVERNAME') !== false ? getenv('DB_SERVERNAME') : 'localhost');
define('DB_USERNAME', getenv('DB_USERNAME') !== false ? getenv('DB_USERNAME') : 'root');
define('DB_PASSWORD', getenv('DB_PASSWORD') !== false ? getenv('DB_PASSWORD') : '');
define('DB_NAME', getenv('DB_NAME') !== false ? getenv('DB_NAME') : 'gespreksbank');
?>
