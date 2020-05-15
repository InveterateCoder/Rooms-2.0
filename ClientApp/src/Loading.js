import React, {useEffect, useRef} from "react";
import Spinner from "react-loading";

export function Loading(props) {
    const div = useRef(null);
    useEffect(() => {
        div.current.focus();
        let handle = setTimeout(() => div.current.style.opacity = 1, 300);
        return () => clearTimeout(handle);
    })
    return <div ref={div} id="loading" className="cover" tabIndex="-1">
        <Spinner id="spinner" type="spinningBubbles" color="#fff" width="100px" />
    </div>
}