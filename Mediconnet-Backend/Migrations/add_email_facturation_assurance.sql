-- Migration: Ajouter email_facturation à la table assurances
-- Date: 2026-02-16
-- Description: Permet l'envoi automatique des factures aux assurances par email

-- Ajouter la colonne email_facturation
ALTER TABLE assurances 
ADD COLUMN email_facturation VARCHAR(255) NULL COMMENT 'Email pour envoi des factures';

-- Mettre à jour les assurances existantes avec des emails par défaut (à modifier)
UPDATE assurances SET email_facturation = 'facturation@cnam.cm' WHERE nom = 'CNAM';
UPDATE assurances SET email_facturation = 'facturation@nsia.cm' WHERE nom = 'NSIA Assurances';
UPDATE assurances SET email_facturation = 'facturation@allianz.cm' WHERE nom = 'Allianz';
UPDATE assurances SET email_facturation = 'facturation@sunu.cm' WHERE nom = 'SUNU Assurances';
UPDATE assurances SET email_facturation = 'facturation@axa.cm' WHERE nom = 'AXA Assurances';
UPDATE assurances SET email_facturation = 'facturation@saham.cm' WHERE nom = 'Saham Assurance';
UPDATE assurances SET email_facturation = 'facturation@cmu.cm' WHERE nom = 'CMU';

-- Ajouter un index pour les recherches
CREATE INDEX IX_assurance_email ON assurances(email_facturation);
