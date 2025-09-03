import { useQuery } from "@tanstack/react-query";
import { getCredentialsOptions  } from "../../../api/@tanstack/react-query.gen";

export const useGetCredentials = () => {
    return useQuery({
        ...getCredentialsOptions({

        }),
        //refetchOnWindowFocus: false,
        //refetchInterval: 1000 * 60 * 2, // 2 minutes
        //staleTime: 1000 * 60, // 1 minute
    });
}
