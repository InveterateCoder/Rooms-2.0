import React from "react";
import Flag from "react-flags";

export function FormGroup(props) {
    const keyPressed = ev => {
        if(ev.which === 13){
            if(!props.error)
                props.add();
        }
    }
    const getRightSide = () => {
        switch (props.type) {
            case "text":
                return <input spellCheck={false} type="text" className={`form-control ${props.error && "error"}`}
                    value={props.value} name={props.name} onChange={props.inputChanged}/>
            case "search":
                return <>
                    <input spellCheck={false} type="search" onKeyPress={keyPressed} placeholder={props.holder} name="langs"
                        className={`form-control ${props.error && props.value && "error"}`}
                        list="search" value={props.value} onChange={props.inputChanged} />
                    <button disabled={props.error} onClick={props.add}
                        className={`ml-2 btn btn-outline${props.error ? "-secondary disabled " : "-primary"} input-group-append`}>
                        {props.addTitle}</button>
                    <datalist id="search">
                        {
                            props.list && props.list.map(opt =>
                                <option key={opt} value={opt} />)
                        }
                    </datalist>
                </>
            case "select":
                return <select className="form-control" value={props.value} onChange={props.onChange}>
                    {
                        Object.entries(props.opts).map(([key, value]) =>
                            <option key={key} value={key}>{value}</option>)
                    }
                </select>
            default: ;
        }
    }
    return <div className="form-group">
        <div className="row">
            <div className={`col-${props.type === "text" || props.type === "search" ? props.lang ? "sm-4" : "sm-3" : "6"}`}>
                {
                    !props.flag
                        ?
                            <label className="h5 pb-2">{props.label}</label>
                        :
                            <>
                                {props.lock && <img src="/img/lock.png" width="48" alt="lock imag"/>}
                                <Flag
                                    name={props.flag}
                                    format="png"
                                    pngSize={48}
                                    shiny={true}
                                    alt={`${props.flag} flag`}
                                    basePath="/img"/>
                            </>
                        
                }
            </div>
            <div className="col">
                <div className={`input-group${props.flag ? " flag" : ""}`}>
                    <div className="input-group-prepend">
                        <span className="input-group-text">
                            <strong className={`${props.type !== "text" && props.type !== "search" ? "invisible" : ""}`}>
                                &ndash;</strong></span>
                    </div>
                    {getRightSide()}
                </div>
            </div>
        </div>
        {
            props.type === "text" && <div className="row">
                <div className="col-sm-3"></div>
                <div className="col">
                    <p className="text-danger"><small>{props.error}</small></p>
                </div>
            </div>
        }
    </div>
}