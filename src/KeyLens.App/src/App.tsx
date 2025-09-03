import { createMooAppBrowserRouter, MooApp } from "@andrewmclachlan/moo-app";
import { routes } from "./Routes";
import { useEffect, useState } from "react";
import { client } from "./api/client.gen";

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

    console.log("client", client);
    console.log("config", client.getConfig());

    const router = createMooAppBrowserRouter(routes);

    return (
        <MooApp clientId={config.audience} client={client.instance} scopes={[config.scope]} name="KeyLens" version={import.meta.env.VITE_REACT_APP_VERSION} copyrightYear={2025} router={router} />
    );
}