import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface PasswordCriteriaState {
  hasMinLength: boolean;
  hasUpperCase: boolean;
  hasLowerCase: boolean;
  hasNumeric: boolean;
  hasSpecial: boolean;
}

export function checkPasswordCriteria(value: string): PasswordCriteriaState {
  const val = value || '';
  return {
    hasMinLength: val.length >= 8,
    hasUpperCase: /[A-Z]/.test(val),
    hasLowerCase: /[a-z]/.test(val),
    hasNumeric: /[0-9]/.test(val),
    hasSpecial: /[^a-zA-Z0-9]/.test(val)
  };
}

export const passwordComplexityValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (!value) {
    return null; // Validators.required xử lý chuỗi rỗng
  }

  const criteria = checkPasswordCriteria(value);
  const isValid = criteria.hasMinLength &&
                  criteria.hasUpperCase &&
                  criteria.hasLowerCase &&
                  criteria.hasNumeric &&
                  criteria.hasSpecial;

  return isValid ? null : { passwordComplexity: criteria };
};

