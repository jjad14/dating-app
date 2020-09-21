import { Injectable } from '@angular/core';
import { User } from '../_models/user';
import { Resolve, Router, ActivatedRouteSnapshot } from '@angular/router';
import { UserService } from '../_services/user.service';
import { AlertifyService } from '../_services/alertify.service';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

// since this is a resolver, we need to provide this just like a guard - app.module.ts
@Injectable()
    export class MemberListResolver implements Resolve<User[]>{

        pageNumber = 1;
        pageSize = 5;

        constructor(private userService: UserService, private router: Router, private alertify: AlertifyService) {}

        // when we resolve we're going to go out to our user service get the user that matches the route parameter that we're aiming to get
        // the rest is to catch the error and return out of this method if we have a problem
        // if there is no problem we're just going to continue on to our route that we're activating,
        // but in this case we have the opportunity to get the data from our routes rather than going out to our user service to get it

        // resolve automatically subscribes to the method, but we do want to catch any errors that occur so we can redirect the user
        // back and also get out of this method
        resolve(route: ActivatedRouteSnapshot): Observable<User[]> {

            return this.userService.getUsers(this.pageNumber, this.pageSize).pipe(
                catchError(error => {
                    this.alertify.error('Problem retrieving data');
                    // route user back to members
                    this.router.navigate(['/home']);
                    // eturn observable of null
                    return of(null);
                })
            )
        }

    }
