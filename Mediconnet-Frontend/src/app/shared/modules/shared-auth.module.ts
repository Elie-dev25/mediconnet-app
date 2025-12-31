/**
 * @deprecated Ce module n'est plus utilisé.
 * Les composants AuthSidebarDecorative et AuthLayoutWrapper ne sont plus référencés.
 * Ce fichier est conservé pour référence mais ne doit plus être utilisé.
 */
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthSidebarDecorativeComponent } from '../components/auth-sidebar-decorative/auth-sidebar-decorative.component';
import { AuthLayoutWrapperComponent } from '../components/auth-layout-wrapper/auth-layout-wrapper.component';

/** @deprecated */
@NgModule({
  imports: [
    CommonModule,
    AuthSidebarDecorativeComponent,
    AuthLayoutWrapperComponent
  ],
  exports: [
    AuthSidebarDecorativeComponent,
    AuthLayoutWrapperComponent
  ]
})
export class SharedAuthModule { }
