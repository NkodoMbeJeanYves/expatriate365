export interface QuickLoginAccount {
  label: string;
  email: string;
  password: string;
}

export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  quickLoginAccounts: [
    { label: 'Super Admin', email: 'super_admin@expatriate365.mu', password: 'Admin@123' },
    { label: 'Président',  email: 'jean.nkodo@acm.mu',   password: 'Password123!' },
    { label: 'Trésorière', email: 'marie.fotso@acm.mu',   password: 'Password123!' },
    { label: 'Secrétaire', email: 'paul.mvondo@acm.mu',   password: 'Password123!' },
    { label: 'Membre',     email: 'alice.biya@acm.mu',    password: 'Password123!' },
  ] as QuickLoginAccount[],
};
