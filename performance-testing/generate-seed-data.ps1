# ============================================================================
# Database Seed Data Generator
# ============================================================================
# Génère des données de test pour mesurer la performance avec une BD remplie

param(
    [int]$PatientCount = 5000,
    [int]$TransactionCount = 100000,
    [string]$DbServer = "localhost",
    [string]$DbName = "mediconnet",
    [string]$DbUser = "root",
    [string]$DbPassword = ""
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

Write-Host "🌱 Database Seed Data Generator - $timestamp" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Generating: $PatientCount patients, $TransactionCount transactions" -ForegroundColor Yellow

# ============================================================================
# GENERATE SQL SEED SCRIPT
# ============================================================================

$seedScript = @"
-- ============================================================================
-- MEDICONNET SEED DATA SCRIPT
-- ============================================================================
-- Generated: $timestamp
-- Patients: $PatientCount
-- Transactions: $TransactionCount

USE $DbName;

-- ============================================================================
-- 1. GENERATE TEST PATIENTS
-- ============================================================================

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_GeneratePatients$$

CREATE PROCEDURE sp_GeneratePatients(IN patient_count INT)
BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE first_names VARCHAR(50) DEFAULT '';
    DECLARE last_names VARCHAR(50) DEFAULT '';
    DECLARE cities VARCHAR(50) DEFAULT '';
    
    -- Insert test patients
    WHILE i < patient_count DO
        SET first_names = CONCAT('Patient_', LPAD(i, 6, '0'));
        SET last_names = CONCAT('TEST_', LPAD(i MOD 100, 3, '0'));
        
        INSERT INTO patients (
            numero_securite_sociale,
            prenom,
            nom,
            date_naissance,
            sexe,
            adresse,
            ville,
            telephone,
            email,
            type_patient,
            active
        ) VALUES (
            CONCAT('999', LPAD(i, 15, '0')),
            first_names,
            last_names,
            DATE_ADD(CURDATE(), INTERVAL -FLOOR(RAND() * 30000) DAY),
            IF(RAND() > 0.5, 'M', 'F'),
            CONCAT(LPAD(i, 4, '0'), ' Test Street'),
            IF(i MOD 3 = 0, 'Douala', IF(i MOD 3 = 1, 'Yaounde', 'Buea')),
            CONCAT('6', LPAD(FLOOR(RAND() * 100000000), 8, '0')),
            CONCAT('patient', LPAD(i, 6, '0'), '@test.com'),
            IF(i MOD 5 = 0, 'HOSPITALISE', 'EXTERNE'),
            1
        );
        
        SET i = i + 1;
        
        -- Progress indicator
        IF i MOD 500 = 0 THEN
            SELECT CONCAT('Generated ', i, ' patients...');
        END IF;
    END WHILE;
    
    SELECT CONCAT('Successfully generated ', patient_count, ' test patients');
END$$

-- ============================================================================
-- 2. GENERATE TEST CONSULTATIONS & TRANSACTIONS
-- ============================================================================

DROP PROCEDURE IF EXISTS sp_GenerateTransactions$$

CREATE PROCEDURE sp_GenerateTransactions(IN transaction_count INT)
BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE patient_id INT DEFAULT 0;
    DECLARE medecin_id INT DEFAULT 0;
    
    -- Get sample users
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET patient_id = 0;
    
    WHILE i < transaction_count DO
        -- Get random patient
        SELECT id INTO patient_id FROM patients ORDER BY RAND() LIMIT 1;
        
        IF patient_id > 0 THEN
            -- Get random doctor
            SELECT id INTO medecin_id FROM utilisateurs 
            WHERE specialite = 'Médecin' ORDER BY RAND() LIMIT 1;
            
            IF medecin_id > 0 THEN
                -- Insert consultation
                INSERT INTO consultations (
                    patient_id,
                    medecin_id,
                    date_consultation,
                    motif,
                    diagnostic,
                    traitement,
                    statut
                ) VALUES (
                    patient_id,
                    medecin_id,
                    DATE_ADD(NOW(), INTERVAL -FLOOR(RAND() * 180) DAY),
                    CONCAT('Consultation test ', FLOOR(RAND() * 1000)),
                    CONCAT('Diagnostic test ', FLOOR(RAND() * 1000)),
                    CONCAT('Traitement test ', FLOOR(RAND() * 1000)),
                    IF(RAND() > 0.5, 'COMPLETE', 'EN_COURS')
                );
            END IF;
        END IF;
        
        SET i = i + 1;
        
        -- Progress indicator
        IF i MOD 10000 = 0 THEN
            SELECT CONCAT('Generated ', i, ' transactions...');
        END IF;
    END WHILE;
    
    SELECT CONCAT('Successfully generated ', transaction_count, ' test transactions');
END$$

DELIMITER ;

-- ============================================================================
-- 3. EXECUTE GENERATION
-- ============================================================================

-- Start timer
SELECT 'Seed data generation started' AS Status, NOW() AS StartTime;

-- Generate patients
CALL sp_GeneratePatients($PatientCount);

-- Generate transactions
CALL sp_GenerateTransactions($TransactionCount);

-- Final stats
SELECT 
    (SELECT COUNT(*) FROM patients) AS TotalPatients,
    (SELECT COUNT(*) FROM consultations) AS TotalConsultations,
    (SELECT COUNT(*) FROM ordonnances) AS TotalPrescriptions,
    NOW() AS CompletedAt;

-- ============================================================================
-- Cleanup procedures
-- ============================================================================

DROP PROCEDURE IF EXISTS sp_GeneratePatients;
DROP PROCEDURE IF EXISTS sp_GenerateTransactions;

"@

# ============================================================================
# SAVE AND EXECUTE
# ============================================================================

$scriptPath = Join-Path $PSScriptRoot "seed-data-$timestamp.sql"
$seedScript | Out-File -FilePath $scriptPath -Encoding UTF8

Write-Host "`n✅ Seed script generated: $scriptPath" -ForegroundColor Green

# Try to execute with mysql CLI
Write-Host "`n🚀 Attempting to execute seed script..." -ForegroundColor Yellow

try {
    $mysqlPath = "mysql"
    
    # Check if mysql is installed
    $mysqlExists = Get-Command mysql -ErrorAction SilentlyContinue
    
    if ($mysqlExists) {
        if ($DbPassword) {
            $output = & mysql -h $DbServer -u $DbUser -p$DbPassword -e "source $scriptPath" 2>&1
        } else {
            $output = & mysql -h $DbServer -u $DbUser -e "source $scriptPath" 2>&1
        }
        
        Write-Host "✅ Seed data executed successfully!" -ForegroundColor Green
        Write-Host $output
    } else {
        Write-Host "⚠️  MySQL CLI not found in PATH" -ForegroundColor Yellow
        Write-Host "`nManual execution steps:" -ForegroundColor Gray
        Write-Host "1. Open MySQL Workbench or command line" -ForegroundColor Gray
        Write-Host "2. Execute: source $scriptPath" -ForegroundColor Gray
        Write-Host "3. Or paste the script manually" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not execute seed script automatically" -ForegroundColor Yellow
    Write-Host "Error: $_" -ForegroundColor Gray
    Write-Host "`nScript saved at: $scriptPath" -ForegroundColor Gray
    Write-Host "Execute manually using MySQL client" -ForegroundColor Gray
}

# ============================================================================
# GENERATE VERIFICATION SCRIPT
# ============================================================================

$verifyScript = @"
-- Verify seed data
SELECT 
    'Patient Count' AS Metric,
    COUNT(*) AS Value
FROM patients
UNION ALL
SELECT 'Consultation Count', COUNT(*) FROM consultations
UNION ALL
SELECT 'Prescription Count', COUNT(*) FROM ordonnances
UNION ALL
SELECT 'Transaction Volume (Est.)', COUNT(*) FROM audit_logs
WHERE created_at >= DATE_SUB(NOW(), INTERVAL 1 HOUR);
"@

$verifyPath = Join-Path $PSScriptRoot "verify-seed-$timestamp.sql"
$verifyScript | Out-File -FilePath $verifyPath -Encoding UTF8

Write-Host "`n📋 Verification script: $verifyPath" -ForegroundColor Cyan
Write-Host "`nTo verify seed data was created:" -ForegroundColor Gray
Write-Host "  mysql -h $DbServer -u $DbUser -e 'source $verifyPath'" -ForegroundColor Gray

Write-Host "`n" -ForegroundColor White
