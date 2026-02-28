import { MooApp } from "@andrewmclachlan/moo-app";
import { Spinner } from "@andrewmclachlan/moo-ds";
import { createRouter } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { client } from "./api/client.gen";
import { routeTree } from "./routeTree.gen.ts";

export const App = () => {

    const [config, setConfig] = useState<any>(null);

    useEffect(() => {
        fetch('/api/v1/config').then(res => res.json()).then(c => {
            setConfig(c);
        })
    }, []);

    if (config === null) {
        return <div>Loading...</div>;
    }

    const router = createRouter({
        routeTree,
        defaultPreload: "intent",
        defaultPreloadStaleTime: 0,
        scrollRestoration: true,
        defaultPendingComponent: Spinner,
    });

    return (
        <MooApp clientId={config.audience} client={client.instance} scopes={[config.scope]} name="KeyLens" version={import.meta.env.VITE_REACT_APP_VERSION} copyrightYear={2025} router={router} />
    );
}
