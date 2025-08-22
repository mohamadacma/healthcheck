import { post } from "./client";

export function askChat(message) {
  return post("/chat", { message }); 
}
