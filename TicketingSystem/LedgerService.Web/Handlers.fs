module LedgerService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open LedgerService.Types
open Newtonsoft.Json

let ``get tickets`` (query : string -> Async<Transaction[] option * System.DateTime>) (``user id`` : string) (ctx : HttpContext) = async {
    let! userAllocations = query ``user id``
    return! OK "" ctx
}