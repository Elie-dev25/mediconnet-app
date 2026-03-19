/**
 * Classe de base pour la gestion des modales
 * Élimine la duplication de la logique de gestion des modales dans les composants
 */
export abstract class ModalManagerBase {
  protected modals: Map<string, boolean> = new Map();

  /**
   * Ouvre une modale
   * @param name - Nom de la modale à ouvrir
   */
  protected openModal(name: string): void {
    this.modals.set(name, true);
  }

  /**
   * Ferme une modale
   * @param name - Nom de la modale à fermer
   */
  protected closeModal(name: string): void {
    this.modals.set(name, false);
  }

  /**
   * Vérifie si une modale est ouverte
   * @param name - Nom de la modale
   * @returns true si la modale est ouverte
   */
  protected isModalOpen(name: string): boolean {
    return this.modals.get(name) || false;
  }

  /**
   * Ferme toutes les modales
   */
  protected closeAllModals(): void {
    this.modals.forEach((_, key) => this.modals.set(key, false));
  }

  /**
   * Bascule l'état d'une modale
   * @param name - Nom de la modale
   */
  protected toggleModal(name: string): void {
    this.modals.set(name, !this.isModalOpen(name));
  }
}
