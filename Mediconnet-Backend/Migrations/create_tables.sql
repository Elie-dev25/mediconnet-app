-- MediConnect - Initial Database Schema
-- This script creates all necessary tables for the application

-- Create Users table
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(255) NOT NULL UNIQUE,
    `Email` VARCHAR(255) NOT NULL UNIQUE,
    `PasswordHash` VARCHAR(500) NOT NULL,
    `FirstName` VARCHAR(100),
    `LastName` VARCHAR(100),
    `PhoneNumber` VARCHAR(20),
    `Department` VARCHAR(100),
    `PrimaryRole` INT NOT NULL,
    `IsActive` BOOLEAN NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL,
    `UpdatedAt` DATETIME,
    INDEX `IX_Username` (`Username`),
    INDEX `IX_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create Roles table
CREATE TABLE IF NOT EXISTS `Roles` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(100) NOT NULL UNIQUE,
    `Description` VARCHAR(500),
    `Permissions` LONGTEXT,
    `IsActive` BOOLEAN NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL,
    INDEX `IX_RoleName` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create AuditLogs table
CREATE TABLE IF NOT EXISTS `AuditLogs` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `Action` VARCHAR(100) NOT NULL,
    `ResourceType` VARCHAR(100) NOT NULL,
    `ResourceId` INT,
    `Details` LONGTEXT,
    `IpAddress` VARCHAR(45),
    `Success` BOOLEAN NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL,
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE,
    INDEX `IX_UserId` (`UserId`),
    INDEX `IX_CreatedAt` (`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create PatientProfiles table
CREATE TABLE IF NOT EXISTS `PatientProfiles` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL UNIQUE,
    `DateOfBirth` DATETIME,
    `Gender` VARCHAR(10),
    `Address` VARCHAR(255),
    `City` VARCHAR(100),
    `PostalCode` VARCHAR(20),
    `Country` VARCHAR(100),
    `PhoneNumber` VARCHAR(20),
    `AlternatePhoneNumber` VARCHAR(20),
    `NationalId` VARCHAR(50),
    `BloodType` VARCHAR(10),
    `Allergies` LONGTEXT,
    `ChronicDiseases` LONGTEXT,
    `GeneralPractitioner` VARCHAR(150),
    `EmergencyContactName` VARCHAR(100),
    `EmergencyContactPhone` VARCHAR(20),
    `EmergencyContactRelation` VARCHAR(50),
    `IsProfileComplete` BOOLEAN NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL,
    `UpdatedAt` DATETIME,
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE,
    INDEX `IX_UserId` (`UserId`),
    INDEX `IX_CreatedAt` (`CreatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create EF Migrations history table
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` NVARCHAR(150) NOT NULL PRIMARY KEY,
    `ProductVersion` NVARCHAR(32) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Insert initial migration record
INSERT IGNORE INTO `__EFMigrationsHistory` VALUES ('20250128_InitialCreate', '8.0.0');

-- Create seed data for roles
INSERT IGNORE INTO `Roles` (`Name`, `Description`, `Permissions`, `IsActive`, `CreatedAt`) VALUES
('Patient', 'Patient role', 'Patient_Read,Patient_ViewAppointments,Patient_BookAppointment,Patient_ViewMedicalRecords', 1, NOW()),
('Doctor', 'Doctor role', 'Doctor_Read,Doctor_ViewPatients,Doctor_EditPatientRecords,Doctor_CreatePrescription', 1, NOW()),
('Nurse', 'Nurse role', 'Nurse_Read,Nurse_ViewPatients,Nurse_RecordVitals,Nurse_ManagePatientCare', 1, NOW()),
('Cashier', 'Cashier role', 'Cashier_Read,Cashier_ViewInvoices,Cashier_ProcessPayment', 1, NOW()),
('Administrator', 'Admin role', 'Admin_Read,Admin_ManageUsers,Admin_ManageRoles,Admin_ManagePermissions', 1, NOW());
