FROM php:8.4-apache-bookworm

# mbstring zit al in dit PHP-image; voeg de MySQL-driver toe.
RUN docker-php-ext-install pdo_mysql

WORKDIR /var/www/html

COPY index.php database.php config.php style.css achtergrond.svg ./
