import { expect, type Page } from '@playwright/test';

export class InvoiceAppPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/');
  }

  async signIn(username: string, password: string) {
    await this.page.getByLabel('Username').fill(username);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
    await expect(this.page.getByRole('heading', { name: 'Invoices' })).toBeVisible();
  }

  async createInvoice(customerName: string, amount: string) {
    await this.page.getByLabel('Customer name').fill(customerName);
    await this.page.getByLabel('Amount').fill(amount);
    await this.page.getByRole('button', { name: 'Create invoice' }).click();
  }

  async submitInvoiceForApproval(customerName: string) {
    const submitButton = this.invoiceRow(customerName)
      .getByRole('button', { name: /^Submit invoice .* for approval$/ });
    const accessibleName = await submitButton.getAttribute('aria-label');
    const invoiceId = accessibleName?.match(/^Submit invoice (.+) for approval$/)?.[1];

    if (!invoiceId) {
      throw new Error(`Could not determine the invoice ID for ${customerName}.`);
    }

    await submitButton.click();
    return invoiceId;
  }

  async openApprovals() {
    await this.page.getByRole('tab', { name: 'Approvals' }).click();
    await expect(this.page.getByRole('heading', { name: 'Pending approvals' })).toBeVisible();
  }

  async approvePendingInvoice(invoiceId: string) {
    await this.page.getByRole('button', { name: `Approve invoice ${invoiceId}` })
      .click();
  }

  decisionError() {
    return this.page.getByRole('alert');
  }

  approvalProgress() {
    return this.page.getByText('High-value approval progress:', { exact: false });
  }

  invoiceRow(customerName: string) {
    return this.page.getByRole('row', { name: new RegExp(customerName) });
  }
}
