const { MongoClient } = require('mongodb');

async function checkLogs() {
    const client = new MongoClient('mongodb://localhost:27017');
    
    try {
        await client.connect();
        console.log('Connected to MongoDB');
        
        const db = client.db('ReservationDb');
        
        // Koleksiyon isimlerini listele
        const collections = await db.listCollections().toArray();
        console.log('Available collections:', collections.map(c => c.name));
        
        const logs = await db.collection('ReservationLogs').find().limit(10).toArray();
        console.log('Reservation Logs:', JSON.stringify(logs, null, 2));
        
        // Rezervasyonları da kontrol edelim
        const reservations = await db.collection('Reservations').find().limit(5).toArray();
        console.log('Recent Reservations:', reservations.map(r => ({ id: r._id, userId: r.UserId, status: r.Status })));
        
    } catch (error) {
        console.error('Error:', error);
    } finally {
        await client.close();
    }
}

checkLogs();
