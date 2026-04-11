Cypress.Commands.add('registerUI', ({ name, email, password, location }) => {
  cy.visit('/register');
  cy.get('#name').clear().type(name);
  cy.get('#email').clear().type(email);
  cy.get('#password').clear().type(password);
  cy.get('#location').clear().type(location);
  cy.contains('button', 'Creează cont').click();
});

Cypress.Commands.add('loginUI', ({ email, password }) => {
  cy.visit('/login');
  cy.get('#email').clear().type(email);
  cy.get('#password').clear().type(password);
  cy.contains('button', 'Autentifică-te').click();
});

Cypress.Commands.add('createDeviceUI', (device) => {
  cy.contains('a', '+ Dispozitiv nou').click();

  cy.get('#name').clear().type(device.name);
  cy.get('#manufacturer').clear().type(device.manufacturer);
  cy.get('#type').select(device.type);
  cy.get('#os').clear().type(device.os);
  cy.get('#osVersion').clear().type(device.osVersion);
  cy.get('#processor').clear().type(device.processor);
  cy.get('#ramAmount').clear().type(String(device.ramAmount));

  cy.contains('button', 'Creează dispozitiv').click();
});