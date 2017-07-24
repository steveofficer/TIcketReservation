module LedgerService.Commands

open MongoDB.Driver
open Types

let ``collection name`` = "Ledger"

let ``record transaction`` (db : IMongoDatabase) (transaction : Transaction) = async {
    let coll = db.GetCollection(``collection name``)
    do! coll.InsertOneAsync(transaction) |> Async.AwaitTask
    return ()
}