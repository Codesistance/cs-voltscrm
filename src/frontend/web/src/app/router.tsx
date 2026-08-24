import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '@/features/auth/routes/LoginPage'
import { SetPasswordPage } from '@/features/auth/routes/SetPasswordPage'
import { ChangePasswordPage } from '@/features/auth/routes/ChangePasswordPage'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { SuperAdminGuard } from '@/features/auth/SuperAdminGuard'
import { AreaGuard } from '@/features/auth/AreaGuard'
import { AreaRedirect } from '@/features/auth/AreaRedirect'
import { PermissionGuard } from '@/features/auth/PermissionGuard'
import { PERMISSIONS } from '@/features/auth/permissions'
import { AreaShell } from '@/features/layout/AreaShell'
import { adminNav } from '@/features/admin/nav'
import { agentNav } from '@/features/agent/nav'
import { portalNav } from '@/features/portal/nav'
import { DashboardPage } from '@/features/dashboard/routes/DashboardPage'
import { AccessPage } from '@/features/admin/access/routes/AccessPage'
import { AuditLogPage } from '@/features/audit/routes/AuditLogPage'
import { InventoryListPage } from '@/features/inventory/routes/InventoryListPage'
import { InventoryDetailPage } from '@/features/inventory/routes/InventoryDetailPage'
import { InventoryFormPage } from '@/features/inventory/routes/InventoryFormPage'
import { InventoryCategoriesPage } from '@/features/inventory/routes/InventoryCategoriesPage'
import { ServicePlanListPage } from '@/features/serviceplans/routes/ServicePlanListPage'
import { ServicePlanDetailPage } from '@/features/serviceplans/routes/ServicePlanDetailPage'
import { ServicePlanFormPage } from '@/features/serviceplans/routes/ServicePlanFormPage'
import { AgentsPage } from '@/features/admin/agents/routes/AgentsPage'
import { CustomerListPage } from '@/features/customers/routes/CustomerListPage'
import { CustomerDetailPage } from '@/features/customers/routes/CustomerDetailPage'
import { CustomerFormPage } from '@/features/customers/routes/CustomerFormPage'
import { SubscriptionListPage } from '@/features/subscriptions/routes/SubscriptionListPage'
import { SubscriptionDetailPage } from '@/features/subscriptions/routes/SubscriptionDetailPage'
import { SubscriptionFormPage } from '@/features/subscriptions/routes/SubscriptionFormPage'
import { SubscriptionEditPage } from '@/features/subscriptions/routes/SubscriptionEditPage'
import { PaymentListPage } from '@/features/payments/routes/PaymentListPage'
import { PaymentDetailPage } from '@/features/payments/routes/PaymentDetailPage'
import { PaymentFormPage } from '@/features/payments/routes/PaymentFormPage'
import { InvoiceListPage } from '@/features/invoices/routes/InvoiceListPage'
import { InvoiceDetailPage } from '@/features/invoices/routes/InvoiceDetailPage'
import { DiscountListPage } from '@/features/discounts/routes/DiscountListPage'
import { GrantDiscountPage } from '@/features/discounts/routes/GrantDiscountPage'
import { InstallmentPlanListPage } from '@/features/installments/routes/InstallmentPlanListPage'
import { InstallmentPlanDetailPage } from '@/features/installments/routes/InstallmentPlanDetailPage'
import { InstallmentPlanFormPage } from '@/features/installments/routes/InstallmentPlanFormPage'
import { ReportsPage } from '@/features/reports/routes/ReportsPage'
import { ImportWizardPage } from '@/features/import/routes/ImportWizardPage'
import { PaymentGatewaySettingsPage } from '@/features/settings/routes/PaymentGatewaySettingsPage'
import { AutoDebitMandatesPage } from '@/features/settings/routes/AutoDebitMandatesPage'
import { TokenVendingPage } from '@/features/settings/routes/TokenVendingPage'
import { AgentHomePage } from '@/features/agent/routes/AgentHomePage'
import { AgentCustomersPage } from '@/features/agent/routes/AgentCustomersPage'
import { AgentMapPage } from '@/features/agent/routes/AgentMapPage'
import { PortalHomePage } from '@/features/portal/routes/PortalHomePage'
import { PortalServicesPage } from '@/features/portal/routes/PortalServicesPage'
import { PortalInvoicesPage } from '@/features/portal/routes/PortalInvoicesPage'
import { PortalPaymentsPage } from '@/features/portal/routes/PortalPaymentsPage'
import { PortalProfilePage } from '@/features/portal/routes/PortalProfilePage'
import { MessageScreen } from '@/shared/components/MessageScreen'
import { PhoenixPage } from '@/features/phoenix/PhoenixPage'
import { config } from '@/app/config'

/** Wraps a single admin page in a permission guard. */
const gated = (permission: string, path: string, element: React.ReactNode) => ({
  element: <PermissionGuard allow={[permission]} />,
  children: [{ path, element }],
})

const notFound = { path: '*', element: <MessageScreen code="404" title="Page not found" /> }

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/set-password', element: <SetPasswordPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { index: true, element: <AreaRedirect /> },
      // Area-agnostic: any authenticated user can (and may be forced to) change their password.
      { path: 'change-password', element: <ChangePasswordPage /> },

      // Phoenix — super-admin account recovery. Registered only when the enable_phoenix flag is on
      // (mirrors the API's Phoenix:Enabled gate); when off the route doesn't exist and falls to 404.
      ...(config.phoenixEnabled
        ? [
            {
              element: <SuperAdminGuard />,
              children: [{ path: 'phoenix', element: <PhoenixPage /> }],
            },
          ]
        : []),

      // Administration area — dynamic, permission-gated pages
      {
        path: 'admin',
        element: <AreaGuard area="Administration" />,
        children: [
          {
            element: <AreaShell navItems={adminNav} homeTo="/admin" />,
            children: [
              { index: true, element: <DashboardPage /> },

              // Customers (read vs. manage)
              {
                element: <PermissionGuard allow={[PERMISSIONS.customersView]} />,
                children: [
                  { path: 'customers', element: <CustomerListPage /> },
                  { path: 'customers/:id', element: <CustomerDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.customersManage]} />,
                children: [
                  { path: 'customers/new', element: <CustomerFormPage /> },
                  { path: 'customers/:id/edit', element: <CustomerFormPage /> },
                ],
              },

              // Agents
              {
                element: <PermissionGuard allow={[PERMISSIONS.agentsView]} />,
                children: [{ path: 'agents', element: <AgentsPage /> }],
              },

              // Inventory (read vs. manage)
              {
                element: <PermissionGuard allow={[PERMISSIONS.inventoryCategoriesManage]} />,
                children: [{ path: 'inventory/categories', element: <InventoryCategoriesPage /> }],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.inventoryView]} />,
                children: [
                  { path: 'inventory', element: <InventoryListPage /> },
                  { path: 'inventory/:id', element: <InventoryDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.inventoryManage]} />,
                children: [
                  { path: 'inventory/new', element: <InventoryFormPage /> },
                  { path: 'inventory/:id/edit', element: <InventoryFormPage /> },
                ],
              },

              // Service plans (read vs. manage)
              {
                element: <PermissionGuard allow={[PERMISSIONS.servicePlansView]} />,
                children: [
                  { path: 'service-plans', element: <ServicePlanListPage /> },
                  { path: 'service-plans/:id', element: <ServicePlanDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.servicePlansManage]} />,
                children: [
                  { path: 'service-plans/new', element: <ServicePlanFormPage /> },
                  { path: 'service-plans/:id/edit', element: <ServicePlanFormPage /> },
                ],
              },

              // Subscriptions (read vs. manage)
              {
                element: <PermissionGuard allow={[PERMISSIONS.subscriptionsView]} />,
                children: [
                  { path: 'subscriptions', element: <SubscriptionListPage /> },
                  { path: 'subscriptions/:id', element: <SubscriptionDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.subscriptionsManage]} />,
                children: [
                  { path: 'subscriptions/new', element: <SubscriptionFormPage /> },
                  { path: 'subscriptions/:id/edit', element: <SubscriptionEditPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.invoicesView]} />,
                children: [
                  { path: 'invoices', element: <InvoiceListPage /> },
                  { path: 'invoices/:id', element: <InvoiceDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.invoicesView]} />,
                children: [
                  { path: 'installments', element: <InstallmentPlanListPage /> },
                  { path: 'installments/:id', element: <InstallmentPlanDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.invoicesManage]} />,
                children: [{ path: 'installments/new', element: <InstallmentPlanFormPage /> }],
              },
              // Payments (read vs. record)
              {
                element: <PermissionGuard allow={[PERMISSIONS.paymentsView]} />,
                children: [
                  { path: 'payments', element: <PaymentListPage /> },
                  { path: 'payments/:id', element: <PaymentDetailPage /> },
                ],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.paymentsRecord]} />,
                children: [{ path: 'payments/new', element: <PaymentFormPage /> }],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.discountsView]} />,
                children: [{ path: 'discounts', element: <DiscountListPage /> }],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.discountsManage]} />,
                children: [{ path: 'discounts/new', element: <GrantDiscountPage /> }],
              },
              gated(PERMISSIONS.reportsView, 'reports', <ReportsPage />),
              {
                element: <PermissionGuard allow={[PERMISSIONS.customersManage]} />,
                children: [{ path: 'import', element: <ImportWizardPage /> }],
              },
              {
                element: <PermissionGuard allow={[PERMISSIONS.settingsManage]} />,
                children: [
                  { path: 'settings/payment-gateways', element: <PaymentGatewaySettingsPage /> },
                  { path: 'settings/auto-debit', element: <AutoDebitMandatesPage /> },
                  { path: 'settings/token-vending', element: <TokenVendingPage /> },
                ],
              },
              gated(PERMISSIONS.accessManage, 'access', <AccessPage />),
              // Audit log — super admins only (also enforced server-side).
              {
                element: <SuperAdminGuard />,
                children: [{ path: 'audit', element: <AuditLogPage /> }],
              },
              notFound,
            ],
          },
        ],
      },

      // Agent area — fixed feature set
      {
        path: 'agent',
        element: <AreaGuard area="Agent" />,
        children: [
          {
            element: <AreaShell navItems={agentNav} homeTo="/agent" />,
            children: [
              { index: true, element: <AgentHomePage /> },
              { path: 'customers', element: <AgentCustomersPage /> },
              { path: 'map', element: <AgentMapPage /> },
              notFound,
            ],
          },
        ],
      },

      // Customer self-service portal — fixed feature set
      {
        path: 'portal',
        element: <AreaGuard area="Customer" />,
        children: [
          {
            element: <AreaShell navItems={portalNav} homeTo="/portal" />,
            children: [
              { index: true, element: <PortalHomePage /> },
              { path: 'services', element: <PortalServicesPage /> },
              { path: 'invoices', element: <PortalInvoicesPage /> },
              { path: 'payments', element: <PortalPaymentsPage /> },
              { path: 'profile', element: <PortalProfilePage /> },
              notFound,
            ],
          },
        ],
      },

      // Shown without area chrome
      {
        path: '403',
        element: (
          <MessageScreen code="403" title="Forbidden" description="You don't have access to this page." />
        ),
      },
      notFound,
    ],
  },
])
