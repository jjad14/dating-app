import { Directive, Input, ViewContainerRef, TemplateRef, OnInit } from '@angular/core';

import { AuthService } from '../_services/auth.service';

@Directive({
  selector: '[appHasRole]' // use with *appHasRole
})
export class HasRoleDirective implements OnInit{

  @Input() appHasRole: string[];
  isVisible = false;

  // ViewContainerRef is a container for two different kinds of views.
  // It can be a component or a can be a template. And we're going to use this to view templates.
  constructor(private viewContainerRef: ViewContainerRef,
              private templateRef: TemplateRef<any>,
              private authService: AuthService) { }

  ngOnInit() {
    const userRoles = this.authService.decodedToken.role as Array<string>;

    // no roles, clear the viewContainerRef
    if (!userRoles) {
      this.viewContainerRef.clear();
    }

    // if user has role needed then render the element
    if (this.authService.roleMatch(this.appHasRole)) {
      if (!this.isVisible) {
        this.isVisible = true;
        this.viewContainerRef.createEmbeddedView(this.templateRef);
      } else {
        this.isVisible = false;
        this.viewContainerRef.clear();
      }
    }


  }
}
