describe('Device Management - Full Demo Flow', () => {
  const now = Date.now();

  const user = {
    name: 'Demo User',
    email: `demo.${now}@example.com`,
    password: 'DemoPass123!',
    location: 'Bucharest'
  };

  const device = {
    name: `Demo Phone ${now}`,
    manufacturer: 'Samsung',
    type: 'phone',
    os: 'Android',
    osVersion: '14',
    processor: 'Snapdragon 8 Gen 2',
    ramAmount: 8
  };

  const bonusDevice = {
    name: `Budget Tablet ${now}`,
    manufacturer: 'Samsung',
    type: 'tablet',
    os: 'Android',
    osVersion: '13',
    processor: 'MediaTek Helio G99',
    ramAmount: 4
  };

  beforeEach(() => {
    cy.clearCookies();
    cy.clearLocalStorage();
  });

  it('covers register, login, search, CRUD, assign/unassign, AI description, logout', () => {
    const waitForDevicesPage = () => {
      cy.get('.page', { timeout: 10000 }).should('have.css', 'opacity', '1');
    };
    
    cy.intercept('POST', '**/api/auth/register').as('registerReq');
    cy.intercept('POST', '**/api/auth/login').as('loginReq');
    cy.intercept('GET', '**/api/devices').as('getDevices');
    cy.intercept('GET', '**/api/devices/search*').as('searchDevices');
    cy.intercept('GET', '**/api/users').as('getUsers');
    cy.intercept('POST', '**/api/devices').as('createDevice');
    cy.intercept('PUT', '**/api/devices/*').as('updateDevice');
    cy.intercept('PUT', '**/api/devices/*/assign').as('assignDevice');
    cy.intercept('PUT', '**/api/devices/*/unassign').as('unassignDevice');
    cy.intercept('DELETE', '**/api/devices/*').as('deleteDevice');
    
    cy.intercept('POST', '**/api/descriptions/generate', {
      statusCode: 200,
      body: {
        description: 'AI demo description: premium business-ready device with solid performance and modern OS.'
      }
    }).as('aiGenerate');
    
    // Register
    cy.registerUI(user);
    cy.wait('@registerReq').its('response.statusCode').should('be.oneOf', [200, 201]);
    cy.url().should('include', '/devices');
    cy.wait('@getDevices');
    cy.wait('@getUsers');
    waitForDevicesPage();
    
    // Logout
    cy.contains('button', 'Deconectare').click();
    cy.url().should('include', '/login');
    
    // Login
    cy.loginUI({ email: user.email, password: user.password });
    cy.wait('@loginReq').its('response.statusCode').should('eq', 200);
    cy.url().should('include', '/devices');
    waitForDevicesPage();
    
    // Create
    cy.createDeviceUI(device);
    cy.wait('@createDevice').its('response.statusCode').should('be.oneOf', [200, 201]);
    cy.url().should('include', '/devices');
    waitForDevicesPage();
    cy.contains('tr', device.name).should('be.visible');

    // Bonus phase - search with ranked results
    cy.createDeviceUI(bonusDevice);
    cy.wait('@createDevice').its('response.statusCode').should('be.oneOf', [200, 201]);
    cy.url().should('include', '/devices');
    waitForDevicesPage();
    cy.contains('tr', bonusDevice.name).should('be.visible');
    
    // Read / Search
    cy.get('#device-search').clear().type(`${now} phone`);
    cy.wait('@searchDevices');
    cy.get('tbody tr').should('have.length', 2);
    cy.get('tbody tr').eq(0).should('contain', device.name);
    cy.get('tbody tr').eq(1).should('contain', bonusDevice.name);
    cy.wait(1200);
    
    // AI Description (from list modal)
    cy.contains('button.device-name-btn', device.name).click();
    cy.wait('@aiGenerate');
    cy.contains('h2', 'Descriere AI').should('be.visible');
    cy.contains('.ai-description', 'AI demo description').should('be.visible');
    cy.wait(1200);
    cy.get('.modal-close').click();
    
    // Assign
    cy.contains('tr', device.name).within(() => {
      cy.contains('button', 'Asignează-mi').click();
    });
    cy.wait('@assignDevice').its('response.statusCode').should('eq', 204);
    
    // Unassign
    cy.contains('tr', device.name).within(() => {
      cy.contains('button', 'Dezasignează').click();
    });
    cy.wait('@unassignDevice').its('response.statusCode').should('eq', 204);
    
    // Update
    cy.contains('tr', device.name).within(() => {
      cy.contains('a', 'Editează').click();
    });
    
    const updatedName = `${device.name} Updated`;
    cy.get('#name').clear().type(updatedName);
    cy.contains('button', 'Salvează modificările').click();
    cy.wait('@updateDevice').its('response.statusCode').should('eq', 204);
    cy.url().should('include', '/devices');
    waitForDevicesPage();
    cy.contains('tr', updatedName).should('be.visible');
    
    // Delete
    cy.on('window:confirm', () => true);
    cy.contains('tr', updatedName).within(() => {
      cy.contains('button', 'Șterge').click();
    });
    cy.wait('@deleteDevice').its('response.statusCode').should('eq', 204);
    cy.wait(800);
    cy.contains('tr', updatedName).should('not.exist');
    
    // Final logout
    cy.contains('button', 'Deconectare').click();
    cy.url().should('include', '/login');
  });
});