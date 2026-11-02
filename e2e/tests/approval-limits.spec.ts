import { expect, test } from '@playwright/test';
import { InvoiceAppPage } from '../pages/invoice-app-page';

const manager = { username: 'demo.approver', password: 'ChangeMe123!' };

test('a Manager sees the over-limit reason and the invoice remains pending after reload', async ({ page }) => {
  const app = new InvoiceAppPage(page);
  const customerName = 'Manager over-limit invoice';

  await app.open();
  await app.signIn(manager.username, manager.password);
  await app.createInvoice(customerName, '50000.01');
  const invoiceId = await app.submitInvoiceForApproval(customerName);
  await app.openApprovals();
  await app.approvePendingInvoice(invoiceId);

  await expect(app.decisionError()).toHaveText(/Manager authority is limited to invoices up to 50,000/);

  await page.reload();
  await expect(app.invoiceRow(customerName)).toContainText('Pending Approval');
});

test('a first high-value Manager approval shows the pending Director second-approval progress', async ({ page }) => {
  const app = new InvoiceAppPage(page);
  const customerName = 'High-value approval progress invoice';

  await app.open();
  await app.signIn(manager.username, manager.password);
  await app.createInvoice(customerName, '500000.01');
  const invoiceId = await app.submitInvoiceForApproval(customerName);
  await app.openApprovals();
  await app.approvePendingInvoice(invoiceId);

  await expect(app.approvalProgress()).toHaveText(
    'High-value approval progress: 1 of 2 approvals recorded. The first Manager approval is pending final approval; a different Director must provide the second approval.');
});
