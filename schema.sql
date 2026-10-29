
/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
DROP TABLE IF EXISTS `AccessSessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AccessSessions` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `DeviceKey` char(64) NOT NULL,
  `Source` varchar(20) NOT NULL,
  `EntryPlaceId` int DEFAULT NULL,
  `StartedAt` datetime(6) NOT NULL,
  `LastSeenAt` datetime(6) NOT NULL,
  `EndedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AccessSessions_DeviceKey` (`DeviceKey`),
  KEY `IX_AccessSessions_EntryPlaceId` (`EntryPlaceId`),
  KEY `IX_AccessSessions_StartedAt` (`StartedAt`),
  CONSTRAINT `FK_AccessSessions_Places_EntryPlaceId` FOREIGN KEY (`EntryPlaceId`) REFERENCES `Places` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `Favorites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Favorites` (
  `UserId` int NOT NULL,
  `PlaceId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserId`,`PlaceId`),
  KEY `IX_Favorites_PlaceId` (`PlaceId`),
  CONSTRAINT `FK_Favorites_Places_PlaceId` FOREIGN KEY (`PlaceId`) REFERENCES `Places` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Favorites_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `Languages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Languages` (
  `Code` varchar(10) NOT NULL,
  `MtCode` varchar(10) NOT NULL,
  `SpeechLang` varchar(10) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL,
  PRIMARY KEY (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `ListenEvents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ListenEvents` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `PlaceId` int NOT NULL,
  `LanguageCode` varchar(10) NOT NULL,
  `UserId` int DEFAULT NULL,
  `ListenedSeconds` int NOT NULL,
  `PlayedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ListenEvents_PlaceId` (`PlaceId`),
  KEY `IX_ListenEvents_UserId_PlayedAt` (`UserId`,`PlayedAt`),
  CONSTRAINT `FK_ListenEvents_Places_PlaceId` FOREIGN KEY (`PlaceId`) REFERENCES `Places` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ListenEvents_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `PlaceTranslations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PlaceTranslations` (
  `PlaceId` int NOT NULL,
  `LanguageCode` varchar(10) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `NarrationText` varchar(4000) DEFAULT NULL,
  `Status` varchar(20) NOT NULL,
  `Source` varchar(20) NOT NULL,
  `SourceHash` char(64) DEFAULT NULL,
  `MachineMatch` decimal(5,4) DEFAULT NULL,
  PRIMARY KEY (`PlaceId`,`LanguageCode`),
  KEY `IX_PT_LanguageCode_Status` (`LanguageCode`,`Status`),
  CONSTRAINT `FK_PlaceTranslations_Languages_LanguageCode` FOREIGN KEY (`LanguageCode`) REFERENCES `Languages` (`Code`) ON DELETE RESTRICT,
  CONSTRAINT `FK_PlaceTranslations_Places_PlaceId` FOREIGN KEY (`PlaceId`) REFERENCES `Places` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `Places`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Places` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Slug` varchar(100) NOT NULL,
  `Kind` varchar(20) NOT NULL,
  `Category` varchar(30) DEFAULT NULL,
  `FoodType` varchar(30) DEFAULT NULL,
  `ParentId` int DEFAULT NULL,
  `Latitude` decimal(9,6) NOT NULL,
  `Longitude` decimal(9,6) NOT NULL,
  `CoverImageUrl` varchar(500) DEFAULT NULL,
  `Status` varchar(20) NOT NULL,
  `SortOrder` int NOT NULL,
  `Address` varchar(300) DEFAULT NULL,
  `PriceRange` varchar(100) DEFAULT NULL,
  `OpeningHours` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Places_Slug` (`Slug`),
  KEY `IX_Places_ParentId_Status_SortOrder` (`ParentId`,`Status`,`SortOrder`),
  KEY `IX_Places_Status_Category` (`Status`,`Category`),
  CONSTRAINT `FK_Places_Places_ParentId` FOREIGN KEY (`ParentId`) REFERENCES `Places` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `CK_Places_Kind_Parent` CHECK ((((`Kind` = _utf8mb4'Destination') and (`ParentId` is null)) or ((`Kind` = _utf8mb4'Food') and (`ParentId` is not null)))),
  CONSTRAINT `CK_Places_ServiceArea` CHECK (((`Latitude` between 10.730 and 10.765) and (`Longitude` between 106.615 and 106.665)))
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `UsageLogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UsageLogs` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `Service` varchar(20) NOT NULL,
  `Characters` int NOT NULL,
  `Chunks` int NOT NULL,
  `Success` tinyint(1) NOT NULL,
  `ErrorCode` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_UsageLogs_Service_CreatedAt` (`Service`,`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `Users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(254) NOT NULL,
  `PasswordHash` varchar(200) NOT NULL,
  `FullName` varchar(100) NOT NULL,
  `Role` varchar(20) NOT NULL,
  `PreferredLanguageCode` varchar(10) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `Visits`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Visits` (
  `UserId` int NOT NULL,
  `PlaceId` int NOT NULL,
  `VisitedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`UserId`,`PlaceId`),
  KEY `IX_Visits_PlaceId` (`PlaceId`),
  CONSTRAINT `FK_Visits_Places_PlaceId` FOREIGN KEY (`PlaceId`) REFERENCES `Places` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Visits_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

