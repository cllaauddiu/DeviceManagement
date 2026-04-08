db = db.getSiblingDB('DeviceManagementDB');

if (!db.getCollectionNames().includes("Devices")) {
    db.createCollection("Devices");
}
if (!db.getCollectionNames().includes("Users")) {
    db.createCollection("Users");
}

if (db.Devices.countDocuments({}) === 0) {
    db.Devices.insertMany([
        { 
            Name: "Galaxy S23", Manufacturer: "Samsung", Type: "phone", 
            OS: "Android", OSVersion: "14", Processor: "Snapdragon 8 Gen 2", 
            RamAmount: 8, Description: "Telefon de serviciu standard" 
        },
        { 
            Name: "iPad Pro", Manufacturer: "Apple", Type: "tablet", 
            OS: "iPadOS", OSVersion: "17", Processor: "M2", 
            RamAmount: 8, Description: "Tableta pentru echipa de design" 
        }
    ]);
    print("Date dummy inserate în Devices.");
} else {
    print("Colecția Devices conține deja date. Se omite inserarea.");
}

if (db.Users.countDocuments({}) === 0) {
    db.Users.insertMany([
        { Name: "Popescu Ion", Role: "Software Engineer", Location: "Cluj-Napoca" },
        { Name: "Ionescu Maria", Role: "Project Manager", Location: "Bucuresti" }
    ]);
    print("Date dummy inserate în Users.");
} else {
    print("Colecția Users conține deja date. Se omite inserarea.");
}