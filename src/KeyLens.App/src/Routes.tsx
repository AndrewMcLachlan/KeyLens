import { type RouteDefinition } from "@andrewmclachlan/moo-app";
import { Layout } from "./Layout";
import { Dashboard } from "./pages/Dashboard/Dashboard";

export const routes: RouteDefinition = {
    layout: {
        path: "/", element: <Layout />, children: {
            dashboard: { path: "", element: <Dashboard /> },
        },
    },
};