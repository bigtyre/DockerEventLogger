CREATE TABLE IF NOT EXISTS `container_images` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `container_events` (
  `id` int NOT NULL AUTO_INCREMENT,
  `time` datetime NOT NULL,
  `type` varchar(30) NOT NULL,
  `container_name` varchar(255) NOT NULL,
  `service_name` varchar(255) DEFAULT NULL,
  `image_id` int DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `container_name` (`container_name`),
  KEY `image_id` (`image_id`),
  KEY `time` (`time`),
  KEY `service_name` (`service_name`),
  CONSTRAINT `container_events_ibfk_1` FOREIGN KEY (`image_id`) REFERENCES `container_images` (`id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
