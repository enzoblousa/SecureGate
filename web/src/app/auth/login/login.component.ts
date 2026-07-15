import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuthService } from '../auth.service';
import { ApiError, ApiValidationError } from '../models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  submitting = false;

  form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
  ) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const { email, password } = this.form.getRawValue();

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigateByUrl('/home');
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;
        this.snackBar.open(this.extractErrorMessage(error), 'Fechar', { duration: 5000 });
      },
    });
  }

  private extractErrorMessage(error: HttpErrorResponse): string {
    const validationError = error.error as ApiValidationError | undefined;
    if (validationError?.errors?.length) {
      return validationError.errors.join(' ');
    }
    const conflictError = error.error as ApiError | undefined;
    if (conflictError?.error) {
      return conflictError.error;
    }
    return 'Não foi possível fazer login. Verifique suas credenciais.';
  }
}
