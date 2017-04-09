module AvailabilityService.Handlers

open Suave
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Types.Requests
open Types.Responses
open Types.Db
open MongoDB.Bson
open Newtonsoft.Json

let ``get event ticket availability`` (query : string -> Async<EventAvailability option * System.DateTime>) (``event id`` : string) (ctx : HttpContext) = async {
    let! (event, asAt) = query ``event id``
    return!
        match event with
        | Some event -> event |> JsonConvert.SerializeObject |> OK <| ctx
        | None -> NOT_FOUND "Event not found" ctx
}